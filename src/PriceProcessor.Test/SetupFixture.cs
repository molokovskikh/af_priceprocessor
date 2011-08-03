using System;
using System.Collections.Generic;
using System.Configuration;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using Test.Support;
using Environment = NHibernate.Cfg.Environment;

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
			var config = new InPlaceConfigurationSource();
			config.Add(typeof (ActiveRecordBase),
				new Dictionary<string, string> {
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},					
                    {Environment.ConnectionStringName, Literals.GetConnectionName()},
					{Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
					{Environment.Hbm2ddlKeyWords, "none"},
					{Environment.ShowSql, "true"}
				});
			ActiveRecordStarter.Initialize(new[] {typeof (TestClient).Assembly, typeof (Document).Assembly}, config);
		}
	}
}