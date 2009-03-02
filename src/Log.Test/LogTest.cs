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
using log4net.Core;

namespace Log.Test
{
	[TestFixture]
	public class LogTest
	{

		private const int _maxIteration = 5;
		private const int _rowBorder = 100;

		[Test(Description="проверка того, какая система логирования быстрее")]
		public void SimpleLogVSlog4net()
		{
			ResetSimpleLogSection();
			ResetLog4NetSection();

			//удаляем лог-файлы с предыдущего запуска
			string[] _logFiles = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.log");
			foreach (string logFile in _logFiles)
				File.Delete(logFile);

			//устанавливаем значение NullText для параметра %ndc и других
			log4net.Util.SystemInfo.NullText = null;

			Stopwatch _simpleLogWatch = Stopwatch.StartNew();
			LogBySimpleLog();
			_simpleLogWatch.Stop();

			Stopwatch _log4netLogWatch = Stopwatch.StartNew();
			LogBylog4net();
			_log4netLogWatch.Stop();

			//Console.WriteLine("_simpleLogWatch  = {0}", _simpleLogWatch.Elapsed);
			//Console.WriteLine("_log4netLogWatch = {0}", _log4netLogWatch.Elapsed);

			//Все хорошо, если SimpleLog быстрее не более чем на 2 секунды
			Assert.That(_log4netLogWatch.Elapsed.TotalSeconds - _simpleLogWatch.Elapsed.TotalSeconds < 2, "SimpleLog работает быстрее");
		}

		public void ResetSimpleLogSection()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			config.AppSettings.Settings["LogDebug"].Value = "false";
			config.Save(ConfigurationSaveMode.Modified);

			// Force a reload of the changed section.
			ConfigurationManager.RefreshSection("appSettings");
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
			bool _needLog = Convert.ToBoolean(ConfigurationManager.AppSettings["LogDebug"]);
			for (int i = 0; i < _maxIteration; i++)
			{
				SimpleLog.Log(_needLog, "LogBySimpleLog", "Итерация {0}", i);
				dtPrice = Dbf.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						SimpleLog.Log(_needLog, "LogBySimpleLog", "Прошли 100 элементов");
				}
			}

			CorrectSimpleLogSection();

			_needLog = Convert.ToBoolean(ConfigurationManager.AppSettings["LogDebug"]);

			for (int i = 0; i < _maxIteration; i++)
			{
				SimpleLog.Log(_needLog, "LogBySimpleLog", "Итерация {0}", i);
				dtPrice = Dbf.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						SimpleLog.Log(_needLog, "LogBySimpleLog", "Прошли 100 элементов");
				}
			}
		}

		public void ResetLog4NetSection()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net.config");
			XmlNode level = doc.SelectSingleNode(@"/log4net/root/level");
			level.Attributes["value"].Value = "FATAL";
			doc.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net.config");
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
				_log.DebugFormat("Итерация {0}", i);
				log4net.NDC.Push(i.ToString());
				dtPrice = Dbf.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						_log.Debug("Прошли 100 элементов");
				}
				log4net.NDC.Pop();
			}

			//Таким образом можно программно изменить уровень логирования для текущего логгера
			//log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
			//h.Root.Level = h.LevelMap["DEBUG"];

			CorrectLog4NetSection();

			for (int i = 0; i < _maxIteration; i++)
			{
				_log.DebugFormat("Итерация {0}", i); ;
				log4net.NDC.Push(i.ToString());
				dtPrice = Dbf.Load("9.dbf");
				int rowIndex = 0;
				foreach (DataRow dr in dtPrice.Rows)
				{
					rowIndex++;
					double summ = Convert.ToDouble(dr["PRCL1"]) + Convert.ToDouble(dr["PRCL2"]);
					if (rowIndex / _rowBorder == 0)
						_log.Debug("Прошли 100 элементов");
				}
				log4net.NDC.Pop();
			}
		}

		[Test(Description="проверка того, как будет отформатировано исключение с помощью Fatal и FatalFormat, и будет ли добалено innerException в вывод")]
		public void innerExceptionLog()
		{
			XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net.config"));
			ILog _log = LogManager.GetLogger(typeof(LogTest));
			try
			{
				testEx();
			}
			catch (Exception ex)
			{
				_log.Fatal("Error", ex);
				_log.FatalFormat("Format Error = {0}", ex);
			}
		}

		public void testEx()
		{
			try
			{
				int i = int.Parse("dsds");
				Console.WriteLine("i = {0}", i);
			}
			catch (Exception ex)
			{
				throw new Exception("Ошибка в testEx", ex);
			}
		}

		[Test(Description = "проверка того, что будет делать SMTPAppender, если не сможет отправить сообщение")]
		public void SMTPAppenderTest()
		{
			XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net_SMTPAppenderTest.config"));
			ILog _log = LogManager.GetLogger(typeof(LogTest));
			_log.Info("это тест");
			/*
			 * Если стоит параметр     <lossy value="false" />, то будет отправлять письма до последнего.
			 * В случае неудачи будет логировать куда настроено
			 */
		}

		[Test(Description = "проверка SmtpAppender с фильтрами")]
		public void SMTPAppederWithFilterTest()
		{
			log4net.Util.LogLog.InternalDebugging = true;
			XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net_SMTPAppederWithFilterTest.config"));
			ILog _log = LogManager.GetLogger(typeof(LogTest));
			_log.Info("это тест");
			using (log4net.NDC.Push("smtp"))
			{
				_log.Error("это ошибка 1" );
				_log.Fatal("это ошибка фатальная ошибка 1");
				//LoggingEvent le = new LoggingEvent(typeof(LogTest), _log.Logger.Repository, _log.Logger.Name, Level.Fatal, "это ошибка фатальная ошибка 1", null);
				//le.Properties["SMTP"] = "true";
				//_log.Logger.Log(le);
			}
			_log.Error("это ошибка 2");
			_log.Fatal("это ошибка фатальная ошибка 2");
			try
			{
				int i = int.Parse("dsds");
				Console.WriteLine("i = {0}", i);
			}
			catch(Exception exception)
			{
				_log.ErrorFormat("Ошибка при разборе строки {0}\r\n{1}", "dsds", exception);
			}
		}

	}
}
