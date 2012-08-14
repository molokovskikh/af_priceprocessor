﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
#if !DEBUG
using System.ServiceProcess;
#else
using System.Windows.Forms;
using Inforoom.PriceProcessor;
#endif
using System.IO;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using log4net.Config;
using log4net.Util;
using Environment = NHibernate.Cfg.Environment;

namespace Inforoom.PriceProcessor
{
	public static class Program
	{
		public static void Main()
		{
			SystemInfo.NullText = null;
			XmlConfigurator.Configure();
			var log = LogManager.GetLogger(typeof(Program));
			try {
				With.DefaultConnectionStringName = Literals.GetConnectionName();
				InitActiveRecord(new[] { typeof(Document).Assembly });
				//устанавливаем значение NullText для параметра %ndc и других
#if DEBUG
				InitDirs(new[] {
					Settings.Default.BasePath,
					Settings.Default.ErrorFilesPath,
					Settings.Default.InboundPath,
					Settings.Default.TempPath,
					Settings.Default.HistoryPath
				});

				var monitor = Monitor.GetInstance();
				monitor.Start();
				MessageBox.Show("Для остановки нажмите Ok...", "PriceProcessor");
				monitor.Stop();
				monitor = null;
#else
				ServiceBase[] ServicesToRun;
				ServicesToRun = new ServiceBase[] 
				{ 
					new PriceProcessorService() 
				};
				ServiceBase.Run(ServicesToRun);
#endif
			}
			catch (Exception e) {
				log.Error("Ошибка запуска сервиса обработки прайс листов", e);
			}
		}

		public static void InitActiveRecord(Assembly[] assemblies)
		{
			var config = new InPlaceConfigurationSource();
			config.PluralizeTableNames = true;
			config.Add(typeof(ActiveRecordBase),
				new Dictionary<string, string> {
					{ Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
					{ Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
					{ Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
					{ Environment.ConnectionStringName, Literals.GetConnectionName() },
					{ Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
					{ Environment.Hbm2ddlKeyWords, "none" }
				});
			ActiveRecordStarter.Initialize(assemblies, config);
		}

		public static void InitDirs(IEnumerable<string> dirs)
		{
			foreach (var dir in dirs) {
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}
		}
	}
}