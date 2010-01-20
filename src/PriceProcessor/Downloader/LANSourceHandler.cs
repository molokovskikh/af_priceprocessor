using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;

namespace Inforoom.Downloader
{
	public class LANSourceHandler : PathSourceHandler
	{
		private string _downloadedFile;

		public LANSourceHandler()
		{
			sourceType = "LAN";
		}

		protected override void GetFileFromSource(PriceSource source)
		{
			try
			{
				var pricePath = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + source.FirmCode.ToString().PadLeft(3, '0') +
				                Path.DirectorySeparatorChar;
				var files = Directory.GetFiles(pricePath, source.PriceMask);

				//������������� ������ ������ �� ����������, ���������� �� �����, ���� �� ������� ����� ����� ������
				var sortedFileList = new SortedList<DateTime, string>();

				foreach (var file in files)
				{
					var fileLastWriteTime = File.GetLastWriteTime(file);
					if (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval)
						sortedFileList.Add(fileLastWriteTime, file);
				}

				//���� � ������ ���� �����, �� ����� ������ � ���������
				if (sortedFileList.Count == 0)
					return;

				var downloadedFileName = sortedFileList.Values[0];
				var downloadedLastWriteTime = sortedFileList.Keys[0];
				var newFile = DownHandlerPath + Path.GetFileName(downloadedFileName);
				if (File.Exists(newFile))
				{
					FileHelper.ClearReadOnly(newFile);
					File.Delete(newFile);
				}
				FileHelper.ClearReadOnly(downloadedFileName);
				_downloadedFile = downloadedFileName;
				try
				{
					File.Copy(downloadedFileName, newFile);
					CurrFileName = newFile;
					CurrPriceDate = downloadedLastWriteTime;
				}
				catch (IOException e)
				{
					var errorCode = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
					var errorAlreadyWas = ErrorPriceLogging.ErrorMessages.ContainsKey(source.PriceItemId) &&
					                      ErrorPriceLogging.ErrorMessages.ContainsValue(e.ToString());
					// ���������, ���� ��� ������ ����������� �������  � �����, � ��� ������ ��� ����������� ��� ����� �����
					if ((errorCode == 32) && errorAlreadyWas)
					{
						throw;
					}
					else if ((errorCode == 32) && !errorAlreadyWas)
					{
						// ���� ��� ������� ����� ������ ��� �� �����������, ��������� �� � �������
						ErrorPriceLogging.ErrorMessages.Add(CurrPriceItemId, e.ToString());
					}
				}
				// ���� ����� ����, ������ ���� ������� ������ � ����� ������� 
				// ��������� �� ������� ��� ����� �����
				if (ErrorPriceLogging.ErrorMessages.ContainsKey(CurrPriceItemId))
					ErrorPriceLogging.ErrorMessages.Remove(CurrPriceItemId);
			}
			catch (Exception e)
			{
				throw new LanSourceHandlerException(e);
			}
		}

		protected override void FileProcessed()
		{
			try
			{
				File.Delete(_downloadedFile);
			}
			catch (Exception e)
			{
				_log.Error(String.Format("������ ��� �������� ����� {0}", _downloadedFile), e);
			}
		}

		protected override DataRow[] GetLikeSources(PriceSource source)
		{
			if (String.IsNullOrEmpty(source.PriceMask))
				return dtSources.Select(String.Format("({0} = {1}) and ({2} is null)",
					SourcesTableColumns.colFirmCode, source.FirmCode,
					SourcesTableColumns.colPriceMask));
			return dtSources.Select(String.Format("({0} = {1}) and ({2} = '{3}')",
				SourcesTableColumns.colFirmCode, source.FirmCode,
				SourcesTableColumns.colPriceMask, source.PriceMask));
		}
	}

	public class LanSourceHandlerException : PathSourceHandlerException
	{
		public LanSourceHandlerException()
		{}

		public LanSourceHandlerException(Exception innerException)
			: base(null, innerException)
		{			
			ErrorMessage = GetShortErrorMessage(innerException);
		}

		protected override string GetShortErrorMessage(Exception e)
		{
			return "LAN: ������� ������ ��� �������������� � ��������";
		}
	}
}
