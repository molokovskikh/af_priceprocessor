using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Data;

namespace Inforoom.Downloader
{
    public class FTPSourceHandler : PathSourceHandler
    {
        public FTPSourceHandler(string sourceType)
            : base(sourceType)
        { }

        protected string[] GetFileList(string FTPDir, string FTPUser, string FTPPassword, string Mask)
        {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPDir);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            if (!String.IsNullOrEmpty(FTPUser))
                request.Credentials = new NetworkCredential(FTPUser, FTPPassword);

            request.Proxy = null;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            List<string> fl = new List<string>();
            while (!reader.EndOfStream)
            {
                string input = reader.ReadLine();
                if (WildcardsHlp.Matched(Mask, input))
                    fl.Add(input);
            }

            reader.Close();
            response.Close();

            return fl.ToArray();
        }


        protected DateTime GetFileDateTime(string FTPDir, string FTPUser, string FTPPassword)
        {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPDir);
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

            if (!String.IsNullOrEmpty(FTPUser))
                request.Credentials = new NetworkCredential(FTPUser, FTPPassword);

            request.Proxy = null;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            DateTime fdt = response.LastModified;

            response.Close();

            return fdt;
        }

        protected void GetFile(string FTPDir, string SaveFileName, string FTPUser, string FTPPassword)
        {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPDir);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            if (!String.IsNullOrEmpty(FTPUser))
                request.Credentials = new NetworkCredential(FTPUser, FTPPassword);

            request.Proxy = null;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            using (FileStream fs = new FileStream(SaveFileName, FileMode.Create))
            {
                CopyStreams(responseStream, fs);
                fs.Close();
            }
            response.Close();
        }


        protected override void GetFileFromSource()
        {
            CurrFileName = String.Empty;
            string PricePath = (string)dtSources.Rows[0][SourcesTable.colPricePath];
            if (!PricePath.StartsWith(@"ftp://", StringComparison.OrdinalIgnoreCase))
                PricePath = @"ftp://" + PricePath;
            if (!PricePath.EndsWith(@"/"))
                PricePath += @"/";
            PricePath += (string)dtSources.Rows[0][SourcesTable.colFTPDir];
            if (!PricePath.EndsWith(@"/"))
                PricePath += @"/";

            string FTPFileName = String.Empty;
            string DownFileName = String.Empty;
            string[] files;
            try
            {
                if (WildcardsHlp.IsWildcards((string)dtSources.Rows[0][SourcesTable.colPriceMask]))
                {
                    files = GetFileList(PricePath, dtSources.Rows[0][SourcesTable.colFTPLogin].ToString(), dtSources.Rows[0][SourcesTable.colFTPPassword].ToString(), dtSources.Rows[0][SourcesTable.colPriceMask].ToString());
                }
                else
                    files = new string[] {(string)dtSources.Rows[0][SourcesTable.colPriceMask]};

                DateTime pdt = ((MySql.Data.Types.MySqlDateTime)dtSources.Rows[0][SourcesTable.colPriceDateTime]).GetDateTime();

                DateTime fdt = pdt;

                foreach(string s in files)
                {
                    DateTime ndt = GetFileDateTime(PricePath + s, dtSources.Rows[0][SourcesTable.colFTPLogin].ToString(), dtSources.Rows[0][SourcesTable.colFTPPassword].ToString());
                    if (ndt.CompareTo(fdt) > 0)
                    {
                        fdt = ndt;
                        FTPFileName = s;
                    }
                }

                if (!String.IsNullOrEmpty(FTPFileName))
                {
                    DownFileName = DownHandlerPath + FTPFileName;
                    GetFile(PricePath + FTPFileName, DownFileName, dtSources.Rows[0][SourcesTable.colFTPLogin].ToString(), dtSources.Rows[0][SourcesTable.colFTPPassword].ToString());
                    CurrFileName = DownFileName;
                    CurrPriceDate = fdt;
                }
            }
            catch (Exception ex)
            {
                Logging(Convert.ToInt32(dtSources.Rows[0][SourcesTable.colPriceCode]), ex.ToString());
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
