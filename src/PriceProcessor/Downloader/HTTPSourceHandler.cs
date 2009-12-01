using System;
using System.Net;
using System.IO;
using System.Data;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Downloader
{
    public class HTTPSourceHandler : PathSourceHandler
    {
        public HTTPSourceHandler()
        {
			sourceType = "HTTP";
		}

        protected static DateTime GetFileDateTime(string HTTPFile, string HTTPUser, string HTTPPassword)
        {
            var request = (HttpWebRequest)WebRequest.Create(HTTPFile);
            request.Method = WebRequestMethods.Http.Head;

            if (!String.IsNullOrEmpty(HTTPUser))
                request.Credentials = new NetworkCredential(HTTPUser, HTTPPassword);

			request.Proxy = null;

            var response = (HttpWebResponse)request.GetResponse();
            var fdt = response.LastModified;

            response.Close();

            return fdt;
        }

        protected static void GetFile(string HTTPFile, string SaveFileName, string HTTPUser, string HTTPPassword)
        {
            var request = (HttpWebRequest)WebRequest.Create(HTTPFile);
            request.Method = WebRequestMethods.Http.Get;

            if (!String.IsNullOrEmpty(HTTPUser))
                request.Credentials = new NetworkCredential(HTTPUser, HTTPPassword);

            request.Proxy = null;

            var response = (HttpWebResponse)request.GetResponse();
            var responseStream = response.GetResponseStream();
            using (var fs = new FileStream(SaveFileName, FileMode.Create))
            {
                FileHelper.CopyStreams(responseStream, fs);
                fs.Close();
            }
            response.Close();
        }


        protected override void GetFileFromSource(PriceSource source)
        {
            CurrFileName = String.Empty;
        	var pricePath = source.PricePath;
            if (!pricePath.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase))
                pricePath = @"http://" + pricePath;

            var httpFileName = source.PriceMask;
            try
            {
				var priceDateTime = source.PriceDateTime;

                var fileLastWriteTime = GetFileDateTime(pricePath, source.HttpLogin, source.HttpPassword);

				if ((fileLastWriteTime.CompareTo(priceDateTime) > 0) && (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval))
                {
                    var downFileName = DownHandlerPath + httpFileName;
                    GetFile(pricePath, downFileName, source.HttpLogin, source.HttpPassword);
                    CurrFileName = downFileName;
                    CurrPriceDate = fileLastWriteTime;
                }

            }
            catch (Exception ex)
            {
                Logging(source.PriceItemId, ex.ToString());
            }
        }

        protected override DataRow[] GetLikeSources(PriceSource source)
        {
        	return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}') and (ISNULL({4}, '') = '{5}') and (ISNULL({6}, '') = '{7}')",
        				SourcesTableColumns.colPricePath, source.PricePath,
        				SourcesTableColumns.colPriceMask, source.PriceMask,
        				SourcesTableColumns.colHTTPLogin, source.HttpLogin,
        				SourcesTableColumns.colHTTPPassword, source.HttpPassword));
        }
    }
}
