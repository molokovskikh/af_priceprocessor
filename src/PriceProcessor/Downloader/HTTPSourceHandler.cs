using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Data;

namespace Inforoom.Downloader
{
    public class HTTPSourceHandler : PathSourceHandler
    {
        public HTTPSourceHandler(string sourceType)
            : base(sourceType)
        { }

        protected DateTime GetFileDateTime(string HTTPFile, string HTTPUser, string HTTPPassword)
        {
            HttpWebRequest request = (HttpWebRequest)FtpWebRequest.Create(HTTPFile);
            request.Method = WebRequestMethods.Http.Head;

            if (!String.IsNullOrEmpty(HTTPUser))
                request.Credentials = new NetworkCredential(HTTPUser, HTTPPassword);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            DateTime fdt = response.LastModified;

            response.Close();

            return fdt;
        }

        protected void GetFile(string HTTPFile, string SaveFileName, string HTTPUser, string HTTPPassword)
        {
            HttpWebRequest request = (HttpWebRequest)FtpWebRequest.Create(HTTPFile);
            request.Method = WebRequestMethods.Http.Get;

            if (!String.IsNullOrEmpty(HTTPUser))
                request.Credentials = new NetworkCredential(HTTPUser, HTTPPassword);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            using (FileStream fs = new FileStream(SaveFileName, FileMode.CreateNew))
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
            if (!PricePath.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase))
                PricePath = @"http://" + PricePath;
            if (!PricePath.EndsWith(@"/"))
                PricePath += @"/";

            string HTTPFileName = (string)dtSources.Rows[0][SourcesTable.colPriceMask];
            string DownFileName = String.Empty;
            try
            {
                DateTime pdt = (DateTime)dtSources.Rows[0][SourcesTable.colPriceDateTime];

                DateTime fdt = GetFileDateTime(PricePath + HTTPFileName, (string)dtSources.Rows[0][SourcesTable.colHTTPLogin], (string)dtSources.Rows[0][SourcesTable.colHTTPPassword]);

                if (fdt.CompareTo(pdt) > 0)
                {
                    DownFileName = DownHandlerPath + HTTPFileName;
                    GetFile(PricePath + HTTPFileName, DownFileName, (string)dtSources.Rows[0][SourcesTable.colHTTPLogin], (string)dtSources.Rows[0][SourcesTable.colHTTPPassword]);
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
            return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}') and ({4} = '{5}')",
                SourcesTable.colPricePath, dtSources.Rows[0][SourcesTable.colPricePath],
                SourcesTable.colPriceMask, dtSources.Rows[0][SourcesTable.colPriceMask],
                SourcesTable.colHTTPLogin, dtSources.Rows[0][SourcesTable.colHTTPLogin],
                SourcesTable.colHTTPPassword, dtSources.Rows[0][SourcesTable.colHTTPPassword]));
        }

    }
}
