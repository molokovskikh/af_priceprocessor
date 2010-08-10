using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Inforoom.Common;
using Inforoom.PriceProcessor.Waybills;
using NHibernate.Cfg;
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
			With.DefaultConnectionStringName = "DB";
			var config = new InPlaceConfigurationSource();
			config.Add(typeof (ActiveRecordBase),
				new Dictionary<string, string> {
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
					{Environment.ConnectionStringName, "DB"},
					{Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
					{Environment.Hbm2ddlKeyWords, "none"}
				});
			ActiveRecordStarter.Initialize(new[] {typeof (TestClient).Assembly, typeof (Document).Assembly}, config);
		}
	}
}