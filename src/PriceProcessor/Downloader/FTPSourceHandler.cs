using System;
using System.IO;
using System.Data;
using LumiSoft.Net.FTP.Client;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;

namespace Inforoom.Downloader
{
    public class FTPSourceHandler : PathSourceHandler
    {
        private FTP_Client m_pFtpClient;

        public FTPSourceHandler()
        {
			sourceType = "FTP";
		}

        protected override void GetFileFromSource()
        {
            CurrFileName = String.Empty;
            string FTPHost = (string)dtSources.Rows[0][SourcesTableColumns.colPricePath];
            if (FTPHost.StartsWith(@"ftp://", StringComparison.OrdinalIgnoreCase))
                FTPHost = FTPHost.Substring(6);
            if (FTPHost.EndsWith(@"/"))
                FTPHost = FTPHost.Substring(0, FTPHost.Length-1);

            string PricePath = (string)dtSources.Rows[0][SourcesTableColumns.colFTPDir];
            if (!PricePath.StartsWith(@"/", StringComparison.OrdinalIgnoreCase))
                PricePath = @"/" + PricePath;

            string FTPFileName = String.Empty;
            string DownFileName = String.Empty;
            string ShortFileName = String.Empty;
            try
            {
                m_pFtpClient = new FTP_Client();
				m_pFtpClient.PassiveMode = Convert.ToByte(dtSources.Rows[0][SourcesTableColumns.colFTPPassiveMode]) == 1;
                m_pFtpClient.Connect(FTPHost, 21);
                m_pFtpClient.Authenticate(dtSources.Rows[0][SourcesTableColumns.colFTPLogin].ToString(), dtSources.Rows[0][SourcesTableColumns.colFTPPassword].ToString());
                m_pFtpClient.SetCurrentDir(PricePath);

                DataSet dsEntries = m_pFtpClient.GetList();

				DateTime priceDateTime = GetPriceDateTime();

                foreach (DataRow drEnt in dsEntries.Tables["DirInfo"].Rows)
                {
                    if (!Convert.ToBoolean(drEnt["IsDirectory"]))
                    {
                        ShortFileName = drEnt["Name"].ToString();
                        if ((WildcardsHelper.IsWildcards((string)dtSources.Rows[0][SourcesTableColumns.colPriceMask]) && WildcardsHelper.Matched((string)dtSources.Rows[0][SourcesTableColumns.colPriceMask], ShortFileName)) ||
                            (String.Compare(ShortFileName, (string)dtSources.Rows[0][SourcesTableColumns.colPriceMask], true) == 0))
                        {
                            DateTime fileLastWriteTime = Convert.ToDateTime(drEnt["Date"]);
							if (
								(
								(fileLastWriteTime.CompareTo(priceDateTime) > 0) 
								&& (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval)
								)
								||
								(
								(fileLastWriteTime.CompareTo(DateTime.Now) > 0)
								&&
								(fileLastWriteTime.Subtract(priceDateTime).TotalMinutes > 0)
								)
								)
                            {
								priceDateTime = fileLastWriteTime;
                                FTPFileName = ShortFileName;
                            }
                        }
                    }
                }

                if (!String.IsNullOrEmpty(FTPFileName))
                {
                    DownFileName = DownHandlerPath + FTPFileName;
                    using (FileStream fs = new FileStream(DownFileName, FileMode.Create))
                    {
                        m_pFtpClient.ReceiveFile(FTPFileName, fs);
                        fs.Close();
                    }
                    CurrFileName = DownFileName;
					CurrPriceDate = priceDateTime;
                }
            }
            catch (Exception ex)
            {
				Logging(Convert.ToUInt64(dtSources.Rows[0][SourcesTableColumns.colPriceItemId]), ex.ToString());
            }
            finally
            { 
                if (m_pFtpClient != null)
                {
                    try
                    {
                        m_pFtpClient.Disconnect();
                    }
                    catch { }
                    m_pFtpClient = null;
                }
            }
        }

        protected override DataRow[] GetLikeSources()
        {
            return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}') and (ISNULL({4}, '') = '{5}') and (ISNULL({6}, '') = '{7}') and (ISNULL({8}, '') = '{9}')",
                SourcesTableColumns.colPricePath, dtSources.Rows[0][SourcesTableColumns.colPricePath],
                SourcesTableColumns.colPriceMask, dtSources.Rows[0][SourcesTableColumns.colPriceMask],
                SourcesTableColumns.colFTPDir, dtSources.Rows[0][SourcesTableColumns.colFTPDir].ToString(),
                SourcesTableColumns.colFTPLogin, dtSources.Rows[0][SourcesTableColumns.colFTPLogin].ToString(),
                SourcesTableColumns.colFTPPassword, dtSources.Rows[0][SourcesTableColumns.colFTPPassword].ToString()));
        }

    }
}
