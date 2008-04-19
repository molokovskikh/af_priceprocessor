using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;

namespace Inforoom.Downloader
{
    public class LANSourceHandler : PathSourceHandler
    {
        public LANSourceHandler()
            : base()
        {
			this.sourceType = "LAN";
		}

        protected override void GetFileFromSource()
        {
            CurrFileName = String.Empty;
            try
            {
				string PricePath = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + dtSources.Rows[0][SourcesTable.colFirmCode].ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar;
                string[] ff = Directory.GetFiles(PricePath, dtSources.Rows[0][SourcesTable.colPriceMask].ToString());
				DateTime priceDateTime = GetPriceDateTime();
                foreach (string fs in ff)
                {
					DateTime fileLastWriteTime = File.GetLastWriteTime(fs);
					if ((fileLastWriteTime.CompareTo(priceDateTime) > 0) && (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval))
                    {
						priceDateTime = fileLastWriteTime;
                        string NewFile = DownHandlerPath + Path.GetFileName(fs);
                        try
                        {
                            //TODO: здесь надо Copy изменить на Move
							if (File.Exists(NewFile))
								File.Delete(NewFile);
							File.Move(fs, NewFile);
							CurrFileName = NewFile;
                            CurrPriceDate = priceDateTime;
                            break;
                        }
                        catch (Exception ex)
                        {
							Logging(Convert.ToUInt64(dtSources.Rows[0][SourcesTable.colPriceItemId]), String.Format("Не удалось скопировать файл {0} : {1}", System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
                        }
                    }
                }
            }
            catch(Exception exDir)
            {
				Logging(Convert.ToUInt64(dtSources.Rows[0][SourcesTable.colPriceItemId]), String.Format("Не удалось получить список файлов : {0}", exDir));
            }
        }

        protected override DataRow[] GetLikeSources()
        {
			if (dtSources.Rows[0][SourcesTable.colPriceMask] is DBNull)
				return dtSources.Select(String.Format("({0} = {1}) and ({2} is null)",
					SourcesTable.colFirmCode, dtSources.Rows[0][SourcesTable.colFirmCode],
					SourcesTable.colPriceMask));
			else
				return dtSources.Select(String.Format("({0} = {1}) and ({2} = '{3}')",
					SourcesTable.colFirmCode, dtSources.Rows[0][SourcesTable.colFirmCode],
					SourcesTable.colPriceMask, dtSources.Rows[0][SourcesTable.colPriceMask]));
        }

    }
}
