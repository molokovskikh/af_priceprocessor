using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Tools;
using Common.Tools.Threading;
#if !DEBUG
using System.ServiceProcess;
#else
using System.Windows.Forms;
using Inforoom.PriceProcessor;
#endif
using System.IO;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using log4net;
using log4net.Config;
using log4net.Util;
using Environment = NHibernate.Cfg.Environment;

namespace Inforoom.PriceProcessor
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			SystemInfo.NullText = null;
			XmlConfigurator.Configure();
			var log = LogManager.GetLogger(typeof(Program));
			try {
				if (args.FirstOrDefault().Match("install")) {
					CommandService.Install();
					return 0;
				}

				ConnectionHelper.DefaultConnectionStringName = "db";
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
				ServicesToRun = new ServiceBase[] {
					new PriceProcessorService()
				};
				ServiceBase.Run(ServicesToRun);
#endif
				return 0;
			}
			catch (Exception e) {
				log.Error("Ошибка запуска сервиса обработки прайс листов", e);
				return 1;
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
					{ Environment.ConnectionStringName, ConnectionHelper.DefaultConnectionStringName },
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