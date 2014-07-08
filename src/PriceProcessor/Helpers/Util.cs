using System;
using System.Linq;
using System.Net;

namespace Inforoom.PriceProcessor.Helpers
{
	public class Util
	{
		public static NetworkCredential GetCredentials(Uri uri)
		{
			if (String.IsNullOrEmpty(uri.UserInfo))
				return null;

			return new NetworkCredential(uri.UserInfo.Split(':').FirstOrDefault(),
				uri.UserInfo.Split(':').Skip(1).FirstOrDefault());
		}
	}
}