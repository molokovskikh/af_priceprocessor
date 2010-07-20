using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
#if !DEBUG
using System.ServiceProcess;
#else
using System.Windows.Forms;
using Inforoom.PriceProcessor.Properties;
#endif
using System.IO;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using log4net.Util;
using Environment=NHibernate.Cfg.Environment;

namespace Inforoom.PriceProcessor
{
	public static class Program
	{
		public static void Main()
		{
			SystemInfo.NullText = null;
			var log = LogManager.GetLogger(typeof(Program));
			try
			{
				With.DefaultConnectionStringName = "DB";
				InitActiveRecord();
				//устанавливаем значение NullText для параметра %ndc и других
#if DEBUG
				InitDirs(new[] {
					Settings.Default.BasePath,
					Settings.Default.ErrorFilesPath,
					Settings.Default.InboundPath,
					Settings.Default.TempPath,
					Settings.Default.HistoryPath
				});

				var monitor = new Monitor();
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
			catch (Exception e)
			{
				log.Error("Ошибка запуска сервиса обработки прайс листов", e);
			}
		}

		private static void InitActiveRecord()
		{
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
			ActiveRecordStarter.Initialize(new[] {typeof (Document).Assembly}, config);
		}

		public static void InitDirs(IEnumerable<string> dirs)
		{
			foreach (var dir in dirs)
			{
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}
		}
	}
}
