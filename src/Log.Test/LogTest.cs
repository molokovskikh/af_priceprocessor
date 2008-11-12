using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using System.Threading;
using Inforoom.Logging;
using log4net;
using System.Diagnostics;
using System.Configuration;
using log4net.Config;
using System.Reflection;
using Inforoom.Data;
using System.Data;
using System.IO;
using System.Xml;

namespace Log.Test
{
	[TestFixture]
	public class LogTest
	{

		private const int _maxIteration = 50;
		private const int _rowBorder = 100;

		[Test(Description="проверка того, какая система логирования быстрее")]
		public void SimpleLogVSlog4net()
		{
			Stopwatch _simpleLogWatch = Stopwatch.StartNew();
			LogBySimpleLog();
			_simpleLogWatch.Stop();

			Stopwatch _log4netLogWatch = Stopwatch.StartNew();
			LogBylog4net();
			_log4netLogWatch.Stop();

			Console.WriteLine("_simpleLogWatch  = {0}", _simpleLogWatch.Elapsed);
			Console.WriteLine("_log4netLogWatch = {0}", _log4netLogWatch.Elapsed);
			Assert.That(_simpleLogWatch.Elapsed, Is.LessThan(_log4netLogWatch.Elapsed), "log4net работает быстрее");
		}

		public void CorrectSimpleLogSection()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			//Console.WriteLine("Config File Name : {0}", config.FilePath);
			config.AppSettings.Settings["LogDebug"].Value = "true";
			config.Save(ConfigurationSaveMode.Modified);

			// Force a reload of the changed section.
            ConfigurationManager.RefreshSection("appSettings");			
		}

		public void LogBySimpleLog()
		{
			DataTable dtPrice;
			for (int i = 0; i < _maxIteration; i++)
			{
				SimpleLog.Log(Convert.ToBoolean(ConfigurationManager.AppSettings["LogDebug"]), "LogBySimpleLog", "Итерация");
				dtPrice = DBF.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						SimpleLog.Log(Convert.ToBoolean(ConfigurationManager.AppSettings["LogDebug"]), "LogBySimpleLog", "Прошли 100 элементов");
				}
			}

			CorrectSimpleLogSection();

			for (int i = 0; i < _maxIteration; i++)
			{
				SimpleLog.Log(Convert.ToBoolean(ConfigurationManager.AppSettings["LogDebug"]), "LogBySimpleLog", "Итерация");
				dtPrice = DBF.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						SimpleLog.Log(Convert.ToBoolean(ConfigurationManager.AppSettings["LogDebug"]), "LogBySimpleLog", "Прошли 100 элементов");
				}
			}
		}

		public void CorrectLog4NetSection()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net.config");
			XmlNode level = doc.SelectSingleNode(@"/log4net/root/level");
			level.Attributes["value"].Value = "DEBUG";
			doc.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net.config");
		}
		
		public void LogBylog4net()
		{
			XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net.config"));
			ILog _log = LogManager.GetLogger(typeof(LogTest));

			DataTable dtPrice;
			for (int i = 0; i < _maxIteration; i++)
			{
				_log.Debug("Итерация");
				dtPrice = DBF.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						_log.Debug("Прошли 100 элементов");
				}
			}

			CorrectLog4NetSection();

			for (int i = 0; i < _maxIteration; i++)
			{
				_log.Debug("Итерация");
				dtPrice = DBF.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						_log.Debug("Прошли 100 элементов");
				}
			}
		}

	}
}
