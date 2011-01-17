using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using FileHelper=Inforoom.PriceProcessor.FileHelper;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Downloader
{
	public class PricePreprocessingException : Exception
	{
		public PricePreprocessingException(string message, string fileName) : base(message)
		{
			FileName = fileName;
		}

		public string FileName { get; set; }
	}

	public enum PriceSourceType : ulong
	{
		EMail = 1,
		Http = 2,
		Ftp = 3,
		Lan = 4
	}

	public abstract class PathSourceHandler : BasePriceSourceHandler
	{
		/// <summary>
		/// ��������� ����������, �������� ��������� � ������� ����������� ��������
		/// </summary>
		protected ArrayList FailedSources = new ArrayList();

		protected override void ProcessData()
		{
			//����� ����� ������� ����������
			FillSourcesTable();
			while (dtSources.Rows.Count > 0)
			{
				DataRow[] drLS = null;
				var currentSource = dtSources.Rows[0];
				var priceSource = new PriceSource(currentSource);
				if (!IsReadyForDownload(priceSource))
				{
					currentSource.Delete();
					dtSources.AcceptChanges();
					continue;
				}
				try
				{
					drLS = GetLikeSources(priceSource);
					try
					{
						CurrFileName = String.Empty;
						GetFileFromSource(priceSource);
						priceSource.UpdateLastCheck();
					}
					catch (PathSourceHandlerException pathException)
					{
						FailedSources.Add(priceSource.PriceItemId);
						DownloadLogEntity.Log(priceSource.SourceTypeId, priceSource.PriceItemId, pathException.ToString(), pathException.ErrorMessage);
					}
					catch (Exception e)
					{
						FailedSources.Add(priceSource.PriceItemId);
						DownloadLogEntity.Log(priceSource.SourceTypeId, priceSource.PriceItemId, e.ToString());
					}

					if (!String.IsNullOrEmpty(CurrFileName))
					{
						var correctArchive = ProcessArchiveIfNeeded(priceSource);
						foreach (var drS in drLS)
						{
							SetCurrentPriceCode(drS);
							string extractFile = null;
							try
							{
								if (!correctArchive)
									throw new PricePreprocessingException("�� ������� ����������� ���� '" + Path.GetFileName(CurrFileName) + "'. ���� ���������", CurrFileName);

								if (!ProcessPriceFile(CurrFileName, out extractFile, priceSource.SourceTypeId))
									throw new PricePreprocessingException("�� ������� ���������� ���� '" + Path.GetFileName(CurrFileName) + "'", CurrFileName);

								LogDownloadedPrice(priceSource.SourceTypeId, Path.GetFileName(CurrFileName), extractFile);
								FileProcessed();
							}
							catch (PricePreprocessingException e)
							{
								LogDownloaderFail(priceSource.SourceTypeId, e.Message, e.FileName);
								FileProcessed();
							}
							catch (Exception e)
							{
								LogDownloaderFail(priceSource.SourceTypeId, e.Message, extractFile);
							}
							finally
							{
								drS.Delete();
							}
						}
						Cleanup();
					}
					else
					{
						foreach (var drDel in drLS)
							drDel.Delete();
					}
				}
				catch (Exception ex)
				{
					var error = String.Empty;
					if (drLS != null && drLS.Length > 1)
					{
						error += String.Join(", ", drLS.Select(r => r[SourcesTableColumns.colPriceCode].ToString()).ToArray());
						drLS.Each(r => FileHelper.Safe(r.Delete));
						error = "��������� : " + error;
					}
					else
					{
						error = String.Format("�������� : {0}", currentSource[SourcesTableColumns.colPriceCode]);
						FileHelper.Safe(currentSource.Delete);
					}
					error += Environment.NewLine + Environment.NewLine + ex;
					LoggingToService(error);
				}
				finally
				{
					FileHelper.Safe(() => dtSources.AcceptChanges());
				}
			}
		}

		/// <summary>
		/// ���������, ����� �� ��������, ������ ������� ����� ���������� � ���������
		/// </summary>
		/// <returns>
		/// true - �������� �����, ����� ����������
		/// false - �������� ��� �� �����, �� ����� ����������
		/// </returns>
		protected bool IsReadyForDownload(PriceSource source)
		{
			// downloadInterval - � ��������
			if (FailedSources.Contains(source.PriceItemId))
			{
				FailedSources.Remove(source.PriceItemId);
				return true;
			}
			return source.IsReadyForDownload();
		}

		private bool ProcessArchiveIfNeeded(PriceSource priceSource)
		{
			bool CorrectArchive = true;
			//�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
			if (ArchiveHelper.IsArchive(CurrFileName))
			{
				if (ArchiveHelper.TestArchive(CurrFileName, priceSource.ArchivePassword))
				{
					try
					{
						FileHelper.ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix, priceSource.ArchivePassword);
					}
					catch (ArchiveHelper.ArchiveException)
					{
						CorrectArchive = false;
					}
				}
				else
					CorrectArchive = false;
			}
			return CorrectArchive;
		}

		protected override void CopyToHistory(UInt64 downloadLogId)
		{
			var HistoryFileName = DownHistoryPath + downloadLogId + Path.GetExtension(CurrFileName);
			FileHelper.Safe(() => File.Copy(CurrFileName, HistoryFileName));
		}

		protected override PriceProcessItem CreatePriceProcessItem(string normalName)
		{
			var item = base.CreatePriceProcessItem(normalName);
			//������������� ����� �������� �����
			item.FileTime = CurrPriceDate;
			return item;
		}

		/// <summary>
		/// �������� ���� �� ���������, ������� �� ������� ������
		/// </summary>
		protected abstract void GetFileFromSource(PriceSource row);

		/// <summary>
		/// �������� �����-�����, � ������� �������� ��������� � ������ � ������
		/// </summary>
		protected abstract DataRow[] GetLikeSources(PriceSource currentSource);

		protected virtual void FileProcessed() { }
	}

	public class PathSourceHandlerException : Exception
	{
		public static string NetworkErrorMessage = "������ �������� ����������";

		public static string ThreadAbortErrorMessage = "�������� ����� ���� ��������";

		public PathSourceHandlerException()
		{ }

		public PathSourceHandlerException(string message, Exception innerException)
			: base(message, innerException)
		{
			ErrorMessage = message;
		}

		public string ErrorMessage { get; set; }

		protected virtual string GetShortErrorMessage(Exception e)
		{
			return NetworkErrorMessage;
		}
	}
}
