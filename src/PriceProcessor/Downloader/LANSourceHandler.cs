using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;
using System.Threading;

namespace Inforoom.Downloader
{
	public class LANSourceHandler : PathSourceHandler
	{
		private string _downloadedFile;

		public LANSourceHandler()
		{
			SourceType = "LAN";
		}

		protected override void GetFileFromSource(PriceSource source)
		{
			try
			{
				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("Try get file from LAN source. FirmCode = {0}, PriceItemId = {1}", source.FirmCode, source.PriceItemId);
				var pricePath = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + source.FirmCode.ToString().PadLeft(3, '0') +
				                Path.DirectorySeparatorChar;
				var files = Directory.GetFiles(pricePath, source.PriceMask);

				//Сортированный список файлов из директории, подходящих по маске, файл со старшей датой будет первым
				var sortedFileList = new SortedList<DateTime, string>();

				foreach (var file in files)
				{
					var fileLastWriteTime = File.GetLastWriteTime(file);
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("File: {0}. LastWriteTime: {1}", file, fileLastWriteTime);
					if (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval)
						sortedFileList.Add(fileLastWriteTime, file);
				}
				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("SortedList count items {0}", sortedFileList.Count);
				//Если в списке есть файлы, то берем первый и скачиваем
				if (sortedFileList.Count == 0)
					return;

				var downloadedFileName = sortedFileList.Values[0];
				var downloadedLastWriteTime = sortedFileList.Keys[0];
				var newFile = DownHandlerPath + Path.GetFileName(downloadedFileName);
				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("Path: {0}", newFile);
				if (File.Exists(newFile))
				{
					FileHelper.ClearReadOnly(newFile);
					File.Delete(newFile);
				}
				FileHelper.ClearReadOnly(downloadedFileName);
				_downloadedFile = downloadedFileName;
				try
				{
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("Copying from {0} to {1}", downloadedFileName, newFile);
					File.Copy(downloadedFileName, newFile);
					CurrFileName = newFile;
					CurrPriceDate = downloadedLastWriteTime;
				}
				catch (IOException e)
				{
					var errorCode = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
					var errorAlreadyWas = ErrorPriceLogging.ErrorMessages.ContainsKey(source.PriceItemId) &&
					                      ErrorPriceLogging.ErrorMessages.ContainsValue(e.ToString());
					// Проверяем, если это ошибка совместного доступа  к файлу, и эта ошибка уже происходила для этого файла
					if ((errorCode == 32) && errorAlreadyWas)
					{
						throw;
					}
					else if ((errorCode == 32) && !errorAlreadyWas)
					{
						// Если для данного файла ошибка еще не происходила, добавляем ее в словарь
						ErrorPriceLogging.ErrorMessages.Add(CurrPriceItemId, e.ToString());
					}
				}
				// Если дошли сюда, значит файл успешно забран и можно удалить 
				// сообщения об ошибках для этого файла
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
				_log.Error(String.Format("Ошибка при удалении файла {0}", _downloadedFile), e);
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
			var threadAbortException = e as ThreadAbortException;
			if (threadAbortException != null)
				return ThreadAbortErrorMessage;
			if (e is IOException)
				return e.Message;
			return NetworkErrorMessage;
		}
	}
}
