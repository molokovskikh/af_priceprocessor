using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using Inforoom.Downloader.Properties;

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
				string PricePath = NormalizeDir(Settings.Default.FTPOptBox) + dtSources.Rows[0][SourcesTable.colFirmCode].ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar;
                string[] ff = Directory.GetFiles(PricePath, (string)dtSources.Rows[0][SourcesTable.colPriceMask]);
                DateTime priceDateTime = ((MySql.Data.Types.MySqlDateTime)dtSources.Rows[0][SourcesTable.colPriceDateTime]).GetDateTime();
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
                            Logging(Convert.ToInt32(dtSources.Rows[0][SourcesTable.colPriceCode]), String.Format("Не удалось скопировать файл {0} : {1}", System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
                        }
                    }
                }
            }
            catch(Exception exDir)
            {
                Logging(Convert.ToInt32(dtSources.Rows[0][SourcesTable.colPriceCode]), String.Format("Не удалось получить список файлов : {0}", exDir));
            }
        }

        protected override DataRow[] GetLikeSources()
        { 
			//TODO: Здесь может ничего не выбраться, в том случае, если параметры установленны не корректно
            return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}')", 
                SourcesTable.colPricePath, dtSources.Rows[0][SourcesTable.colPricePath],
                SourcesTable.colPriceMask, dtSources.Rows[0][SourcesTable.colPriceMask]));
        }

    }
}
