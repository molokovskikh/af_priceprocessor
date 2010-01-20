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

        protected static DateTime GetFileDateTime(string httpFile, string httpUser, string httpPassword)
        {
            var request = (HttpWebRequest)WebRequest.Create(httpFile);
            request.Method = WebRequestMethods.Http.Head;

            if (!String.IsNullOrEmpty(httpUser))
                request.Credentials = new NetworkCredential(httpUser, httpPassword);

			request.Proxy = null;

            var response = (HttpWebResponse)request.GetResponse();
			var fdt = response.LastModified;

            response.Close();

            return fdt;
        }

        protected static void GetFile(string httpFile, string saveFileName, string httpUser, string httpPassword)
        {
            var request = (HttpWebRequest)WebRequest.Create(httpFile);
            request.Method = WebRequestMethods.Http.Get;

            if (!String.IsNullOrEmpty(httpUser))
                request.Credentials = new NetworkCredential(httpUser, httpPassword);

            request.Proxy = null;

            var response = (HttpWebResponse)request.GetResponse();
            var responseStream = response.GetResponseStream();
            using (var fs = new FileStream(saveFileName, FileMode.Create))
            {
                FileHelper.CopyStreams(responseStream, fs);
                fs.Close();
            }
            response.Close();
        }


		protected override void GetFileFromSource(PriceSource source)
		{
			try
			{
				var pricePath = source.PricePath;
				if (!pricePath.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase))
					pricePath = @"http://" + pricePath;

				var httpFileName = source.PriceMask;
				var priceDateTime = source.PriceDateTime;

				if (pricePath[pricePath.Length - 1].ToString() != "/")
					pricePath += "/";
				var httpUrl = pricePath + httpFileName;
				var fileLastWriteTime = GetFileDateTime(httpUrl, source.HttpLogin, source.HttpPassword);
				var downloadInterval = Settings.Default.FileDownloadInterval;
#if DEBUG
				downloadInterval = -1;
				fileLastWriteTime = DateTime.Now;
#endif
				if ((fileLastWriteTime.CompareTo(priceDateTime) > 0) &&
				    (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > downloadInterval))
				{
					var downFileName = DownHandlerPath + httpFileName;
					GetFile(httpUrl, downFileName, source.HttpLogin, source.HttpPassword);
					CurrFileName = downFileName;
					CurrPriceDate = fileLastWriteTime;
				}
			}
			catch (Exception e)
			{
				throw new HttpSourceHandlerException(e);
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

	public class HttpSourceHandlerException : PathSourceHandlerException
	{
		public static string ErrorMessageForbidden = "ƒоступ запрещен. »спользуютс€ недействительные учетные данные.";
		public static string ErrorMessageUnauthorized = "Ќедостаточно прав, дл€ получени€ запрошеного документа.";

		public HttpSourceHandlerException()
		{ }

		public HttpSourceHandlerException(Exception innerException)
			: base(null, innerException)
		{
			ErrorMessage = GetShortErrorMessage(innerException);
		}

		protected override string GetShortErrorMessage(Exception e)
		{
			var message = String.Empty;
			if (e is WebException)
			{
				message += "HTTP: ";
				var webException = e as WebException;
				var webResponse = webException.Response as HttpWebResponse;
				if (webResponse != null)
				{
					switch (webResponse.StatusCode)
					{
						case HttpStatusCode.Unauthorized:
							{
								message += ErrorMessageUnauthorized;
								break;
							}
						case HttpStatusCode.Forbidden:
							{
								message += ErrorMessageForbidden;
								break;
							}
						default:
							{
								message += NetworkErrorMessage;
								break;
							}
					}
				}
				else
					message += NetworkErrorMessage;
			}
			return message;
		}
	}
}
