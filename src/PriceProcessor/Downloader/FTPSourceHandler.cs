using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Data;
using LumiSoft.Net.FTP.Client;

namespace Inforoom.Downloader
{
    public class FTPSourceHandler : PathSourceHandler
    {
        private FTP_Client m_pFtpClient = null;

        public FTPSourceHandler()
            : base()
        {
			this.sourceType = "FTP";
		}

        protected override void GetFileFromSource()
        {
            CurrFileName = String.Empty;
            string FTPHost = (string)dtSources.Rows[0][SourcesTable.colPricePath];
            if (FTPHost.StartsWith(@"ftp://", StringComparison.OrdinalIgnoreCase))
                FTPHost = FTPHost.Substring(6);
            if (FTPHost.EndsWith(@"/"))
                FTPHost = FTPHost.Substring(0, FTPHost.Length-1);

            string PricePath = (string)dtSources.Rows[0][SourcesTable.colFTPDir];
            if (!PricePath.StartsWith(@"/", StringComparison.OrdinalIgnoreCase))
                PricePath = @"/" + PricePath;

            string FTPFileName = String.Empty;
            string DownFileName = String.Empty;
            string ShortFileName = String.Empty;
            try
            {
                m_pFtpClient = new FTP_Client();
				m_pFtpClient.PassiveMode = Convert.ToByte(dtSources.Rows[0][SourcesTable.colFTPPassiveMode]) == 1;
                m_pFtpClient.Connect(FTPHost, 21);
                m_pFtpClient.Authenticate(dtSources.Rows[0][SourcesTable.colFTPLogin].ToString(), dtSources.Rows[0][SourcesTable.colFTPPassword].ToString());
                m_pFtpClient.SetCurrentDir(PricePath);

                DataSet dsEntries = m_pFtpClient.GetList();

                DateTime pdt = ((MySql.Data.Types.MySqlDateTime)dtSources.Rows[0][SourcesTable.colPriceDateTime]).GetDateTime();

                DateTime fdt = pdt;

                foreach (DataRow drEnt in dsEntries.Tables["DirInfo"].Rows)
                {
                    if (!Convert.ToBoolean(drEnt["IsDirectory"]))
                    {
                        ShortFileName = drEnt["Name"].ToString();
                        if ((WildcardsHlp.IsWildcards((string)dtSources.Rows[0][SourcesTable.colPriceMask]) && WildcardsHlp.Matched((string)dtSources.Rows[0][SourcesTable.colPriceMask], ShortFileName)) ||
                            (String.Compare(ShortFileName, (string)dtSources.Rows[0][SourcesTable.colPriceMask], true) == 0))
                        {
                            DateTime ndt = Convert.ToDateTime(drEnt["Date"]);
                            if (ndt.CompareTo(fdt) > 0)
                            {
                                fdt = ndt;
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
                    CurrPriceDate = fdt;
                }
            }
            catch (Exception ex)
            {
                Logging(Convert.ToInt32(dtSources.Rows[0][SourcesTable.colPriceCode]), ex.ToString());
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
                SourcesTable.colPricePath, dtSources.Rows[0][SourcesTable.colPricePath],
                SourcesTable.colPriceMask, dtSources.Rows[0][SourcesTable.colPriceMask],
                SourcesTable.colFTPDir, dtSources.Rows[0][SourcesTable.colFTPDir].ToString(),
                SourcesTable.colFTPLogin, dtSources.Rows[0][SourcesTable.colFTPLogin].ToString(),
                SourcesTable.colFTPPassword, dtSources.Rows[0][SourcesTable.colFTPPassword].ToString()));
        }

    }
}
