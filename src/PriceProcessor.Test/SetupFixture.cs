using System;
using System.IO;
using Common.MySql;
using Inforoom.Common;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate;
using NUnit.Framework;
using Test.Support;
using log4net.Config;

namespace PriceProcessor.Test
{
	[SetUpFixture]
	public class SetupFixture
	{
		[SetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			ConnectionHelper.DefaultConnectionStringName = "db";
			With.DefaultConnectionStringName = "db";
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";
			//мы не должны обращаться к настроящему ftp, вместо этого нужно использовать директорию для эмуляции
			FtpDownloader.UseStub = true;
			var inboundPath = Path.GetFullPath(@"..\..\..\PriceProcessor.Test\Data\Inbound\");
			if (!Directory.Exists(inboundPath))
				Directory.CreateDirectory(inboundPath);
			Settings.Default["InboundPath"] = inboundPath;
			Program.InitActiveRecord(new[] { typeof(TestClient).Assembly, typeof(Document).Assembly });
		}
	}
}