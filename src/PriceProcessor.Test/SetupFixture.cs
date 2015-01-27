﻿using System;
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
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";
			With.DefaultConnectionStringName = Literals.GetConnectionName();
			//мы не должны обращаться к настроящему ftp, вместо этого нужно использовать директорию для эмуляции
			FtpDownloader.UseStub = true;
			Program.InitActiveRecord(new[] { typeof(TestClient).Assembly, typeof(Document).Assembly });
		}
	}
}