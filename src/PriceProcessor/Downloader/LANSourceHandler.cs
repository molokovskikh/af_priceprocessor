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
                string PricePath = NormalizeDir((string)dtSources.Rows[0][SourcesTable.colPricePath]);
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
                            //TODO: ����� ���� Copy �������� �� Move
                            if (File.Exists(NewFile))
                                File.Delete(NewFile);
                            File.Move(fs, NewFile);
                            CurrFileName = NewFile;
                            CurrPriceDate = priceDateTime;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logging(Convert.ToInt32(dtSources.Rows[0][SourcesTable.colPriceCode]), String.Format("�� ������� ����������� ���� {0} : {1}", System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
                        }
                    }
                }
            }
            catch(Exception exDir)
            {
                Logging(Convert.ToInt32(dtSources.Rows[0][SourcesTable.colPriceCode]), String.Format("�� ������� �������� ������ ������ : {0}", exDir));
            }
        }

        protected override DataRow[] GetLikeSources()
        { 
			//TODO: ����� ����� ������ �� ���������, � ��� ������, ���� ��������� ������������ �� ���������
            return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}')", 
                SourcesTable.colPricePath, dtSources.Rows[0][SourcesTable.colPricePath],
                SourcesTable.colPriceMask, dtSources.Rows[0][SourcesTable.colPriceMask]));
        }

    }
}
