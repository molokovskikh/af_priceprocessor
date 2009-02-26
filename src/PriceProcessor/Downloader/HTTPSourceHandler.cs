using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Data;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Downloader
{
    public class HTTPSourceHandler : PathSourceHandler
    {
        public HTTPSourceHandler()
            : base()
        {
			this.sourceType = "HTTP";
		}

        protected DateTime GetFileDateTime(string HTTPFile, string HTTPUser, string HTTPPassword)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HTTPFile);
            request.Method = WebRequestMethods.Http.Head;

            if (!String.IsNullOrEmpty(HTTPUser))
                request.Credentials = new NetworkCredential(HTTPUser, HTTPPassword);

			request.Proxy = null;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            DateTime fdt = response.LastModified;

            response.Close();

            return fdt;
        }

        protected void GetFile(string HTTPFile, string SaveFileName, string HTTPUser, string HTTPPassword)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HTTPFile);
            request.Method = WebRequestMethods.Http.Get;

            if (!String.IsNullOrEmpty(HTTPUser))
                request.Credentials = new NetworkCredential(HTTPUser, HTTPPassword);

            request.Proxy = null;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            using (FileStream fs = new FileStream(SaveFileName, FileMode.Create))
            {
                FileHelper.CopyStreams(responseStream, fs);
                fs.Close();
            }
            response.Close();
        }


        protected override void GetFileFromSource()
        {
            CurrFileName = String.Empty;
            string PricePath = dtSources.Rows[0][SourcesTableColumns.colPricePath].ToString().Trim();
            if (!PricePath.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase))
                PricePath = @"http://" + PricePath;

            string HTTPFileName = (string)dtSources.Rows[0][SourcesTableColumns.colPriceMask];
            string DownFileName = String.Empty;
            try
            {
				DateTime priceDateTime = GetPriceDateTime();

                DateTime fileLastWriteTime = GetFileDateTime(PricePath, dtSources.Rows[0][SourcesTableColumns.colHTTPLogin].ToString(), dtSources.Rows[0][SourcesTableColumns.colHTTPPassword].ToString());

				if ((fileLastWriteTime.CompareTo(priceDateTime) > 0) && (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval))
                {
                    DownFileName = DownHandlerPath + HTTPFileName;
                    GetFile(PricePath, DownFileName, dtSources.Rows[0][SourcesTableColumns.colHTTPLogin].ToString(), dtSources.Rows[0][SourcesTableColumns.colHTTPPassword].ToString());
                    CurrFileName = DownFileName;
                    CurrPriceDate = fileLastWriteTime;
                }

            }
            catch (Exception ex)
            {
                Logging(Convert.ToUInt64(dtSources.Rows[0][SourcesTableColumns.colPriceItemId]), ex.ToString());
            }
        }

        protected override DataRow[] GetLikeSources()
        {
            return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}') and (ISNULL({4}, '') = '{5}') and (ISNULL({6}, '') = '{7}')",
                SourcesTableColumns.colPricePath, dtSources.Rows[0][SourcesTableColumns.colPricePath],
                SourcesTableColumns.colPriceMask, dtSources.Rows[0][SourcesTableColumns.colPriceMask],
                SourcesTableColumns.colHTTPLogin, dtSources.Rows[0][SourcesTableColumns.colHTTPLogin],
                SourcesTableColumns.colHTTPPassword, dtSources.Rows[0][SourcesTableColumns.colHTTPPassword]));
        }

    }
}
