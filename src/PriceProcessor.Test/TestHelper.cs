using System.IO;
using Common.Tools;

namespace PriceProcessor.Test
{
	public class TestHelper
	{
		public static void InitDirs(params string[] dirs)
		{
			dirs.Each(dir => {
			          	if (Directory.Exists(dir))
			          		Directory.Delete(dir, true);
			          	Directory.CreateDirectory(dir);
			          });
		}
	}
}
