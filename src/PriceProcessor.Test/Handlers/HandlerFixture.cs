using System;
using System.Data;
using System.IO;
using Common.MySql;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class HandlerFixture
	{
		[Test]
		public void Get_sources()
		{
			With.Connection(c => {
				var adapter = new MySqlDataAdapter(
					BaseSourceHandler.GetSourcesCommand("HTTP"),
					c);
				var table = new DataTable();
				adapter.Fill(table);
			});
		}

		public class TestSourceHandler : BaseSourceHandler
		{
			public override void ProcessData()
			{
			}

			public void Process()
			{
				CreateDirectoryPath();
			}
		}

		private void ClearDir(string dirName)
		{
			foreach (var file in Directory.GetFiles(dirName))
				File.Delete(file);

			foreach (var subDir in Directory.GetDirectories(dirName))
				Directory.Delete(subDir, true);
		}

		[Test(Description = "Проверка создания временной папки для загрузки у наследника BaseSourceHandler")]
		public void CreateDownloadTempFolder()
		{
			ClearDir(Settings.Default.TempPath);

			var handler = new TestSourceHandler();
			handler.Process();

			Assert.IsTrue(Directory.Exists(Path.Combine(Settings.Default.TempPath, handler.GetType().Name)), "Не создана временная папка для работы sourceHadler");
		}
	}
}