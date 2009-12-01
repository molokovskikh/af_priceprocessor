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

        protected override void GetFileFromSource(PriceSource source)
        {
            CurrFileName = String.Empty;
        	var ftpHost = source.PricePath;
            if (ftpHost.StartsWith(@"ftp://", StringComparison.OrdinalIgnoreCase))
                ftpHost = ftpHost.Substring(6);
            if (ftpHost.EndsWith(@"/"))
                ftpHost = ftpHost.Substring(0, ftpHost.Length-1);

            var pricePath = source.FtpDir;
            if (!pricePath.StartsWith(@"/", StringComparison.OrdinalIgnoreCase))
                pricePath = @"/" + pricePath;

            var FTPFileName = String.Empty;
            var DownFileName = String.Empty;
            var ShortFileName = String.Empty;
            try
            {
                m_pFtpClient = new FTP_Client();
            	m_pFtpClient.PassiveMode = source.FtpPassiveMode;
                m_pFtpClient.Connect(ftpHost, 21);
                m_pFtpClient.Authenticate(source.FtpLogin, source.FtpPassword);
                m_pFtpClient.SetCurrentDir(pricePath);

                DataSet dsEntries = m_pFtpClient.GetList();

				DateTime priceDateTime = source.PriceDateTime;

                foreach (DataRow drEnt in dsEntries.Tables["DirInfo"].Rows)
                {
                    if (!Convert.ToBoolean(drEnt["IsDirectory"]))
                    {
                        ShortFileName = drEnt["Name"].ToString();
                        if ((WildcardsHelper.IsWildcards(source.PriceMask) && WildcardsHelper.Matched(source.PriceMask, ShortFileName)) ||
                            (String.Compare(ShortFileName, source.PriceMask, true) == 0))
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
				Logging(source.PriceItemId, ex.ToString());
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

        protected override DataRow[] GetLikeSources(PriceSource source)
        {
        	return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}') and (ISNULL({4}, '') = '{5}') and (ISNULL({6}, '') = '{7}') and (ISNULL({8}, '') = '{9}')",
                SourcesTableColumns.colPricePath, source.PricePath,
                SourcesTableColumns.colPriceMask, source.PriceMask,
                SourcesTableColumns.colFTPDir, source.FtpDir,
                SourcesTableColumns.colFTPLogin, source.FtpLogin,
                SourcesTableColumns.colFTPPassword, source.FtpPassword));
        }
    }
}
