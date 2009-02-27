using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;

namespace Inforoom.Downloader
{
    public class LANSourceHandler : PathSourceHandler
    {
        public LANSourceHandler()
        {
			sourceType = "LAN";
		}

        protected override void GetFileFromSource()
        {
            CurrFileName = String.Empty;
            try
            {
				var PricePath = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + dtSources.Rows[0][SourcesTableColumns.colFirmCode].ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar;
                var ff = Directory.GetFiles(PricePath, dtSources.Rows[0][SourcesTableColumns.colPriceMask].ToString());

				//������������� ������ ������ �� ����������, ���������� �� �����, ���� �� ������� ����� ����� ������
				var sortedFileList = new SortedList<DateTime, string>();

				foreach (var fs in ff)
				{
					var fileLastWriteTime = File.GetLastWriteTime(fs);
					if (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval)
						try
						{
							sortedFileList.Add(fileLastWriteTime, fs);
						}
						catch (ArgumentException)
						{
						}
				}

				//���� � ������ ���� �����, �� ����� ������ � ���������
				if (sortedFileList.Count > 0)
				{
					var downloadedFileName = sortedFileList.Values[0];
					var downloadedLastWriteTime = sortedFileList.Keys[0];
					var NewFile = DownHandlerPath + Path.GetFileName(downloadedFileName);
					try
					{
						if (File.Exists(NewFile))
						{
							FileHelper.ClearReadOnly(NewFile);
							File.Delete(NewFile);
						}
						FileHelper.ClearReadOnly(downloadedFileName);
						File.Move(downloadedFileName, NewFile);
						CurrFileName = NewFile;
						CurrPriceDate = downloadedLastWriteTime;
					}
					catch (Exception ex)
					{
						Logging(Convert.ToUInt64(dtSources.Rows[0][SourcesTableColumns.colPriceItemId]), String.Format("�� ������� ����������� ���� {0} : {1}", System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
					}
				}
            }
            catch(Exception exDir)
            {
				Logging(Convert.ToUInt64(dtSources.Rows[0][SourcesTableColumns.colPriceItemId]), String.Format("�� ������� �������� ������ ������ : {0}", exDir));
            }
        }

        protected override DataRow[] GetLikeSources()
        {
        	if (dtSources.Rows[0][SourcesTableColumns.colPriceMask] is DBNull)
				return dtSources.Select(String.Format("({0} = {1}) and ({2} is null)",
					SourcesTableColumns.colFirmCode, dtSources.Rows[0][SourcesTableColumns.colFirmCode],
					SourcesTableColumns.colPriceMask));
        	return dtSources.Select(String.Format("({0} = {1}) and ({2} = '{3}')",
        	                                      SourcesTableColumns.colFirmCode, dtSources.Rows[0][SourcesTableColumns.colFirmCode],
        	                                      SourcesTableColumns.colPriceMask, dtSources.Rows[0][SourcesTableColumns.colPriceMask]));
        }
    }
}
