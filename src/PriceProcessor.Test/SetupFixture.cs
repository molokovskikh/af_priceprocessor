using System;
using Common.MySql;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test
{
	[SetUpFixture]
	public class SetupFixture
	{
		[SetUp]
		public void Setup()
		{
			ArchiveHelper.SevenZipExePath = @".\7zip\7z.exe";
			With.DefaultConnectionStringName = Literals.GetConnectionName();
			Program.InitActiveRecord(new[] {typeof (TestClient).Assembly, typeof (Document).Assembly});
		}
	}
}