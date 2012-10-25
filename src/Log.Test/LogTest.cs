using System;
using System.Collections.Generic;
using System.Text;
using Common.Tools;
using NUnit.Framework;
using System.Threading;
using log4net;
using System.Diagnostics;
using System.Configuration;
using log4net.Config;
using System.Reflection;
using System.Data;
using System.IO;
using System.Xml;
using log4net.Core;

namespace Log.Test
{
	[TestFixture]
	public class LogTest
	{
		[Test(Description = "проверка того, как будет отформатировано исключение с помощью Fatal и FatalFormat, и будет ли добалено innerException в вывод")]
		public void innerExceptionLog()
		{
			XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "log4net.config"));
			ILog _log = LogManager.GetLogger(typeof(LogTest));
			try {
				testEx();
			}
			catch (Exception ex) {
				_log.Fatal("Error", ex);
				_log.FatalFormat("Format Error = {0}", ex);
			}
		}

		public void testEx()
		{
			try {
				int i = int.Parse("dsds");
				Console.WriteLine("i = {0}", i);
			}
			catch (Exception ex) {
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
			using (log4net.NDC.Push("smtp")) {
				_log.Error("это ошибка 1");
				_log.Fatal("это ошибка фатальная ошибка 1");
				//LoggingEvent le = new LoggingEvent(typeof(LogTest), _log.Logger.Repository, _log.Logger.Name, Level.Fatal, "это ошибка фатальная ошибка 1", null);
				//le.Properties["SMTP"] = "true";
				//_log.Logger.Log(le);
			}
			_log.Error("это ошибка 2");
			_log.Fatal("это ошибка фатальная ошибка 2");
			try {
				int i = int.Parse("dsds");
				Console.WriteLine("i = {0}", i);
			}
			catch (Exception exception) {
				_log.ErrorFormat("Ошибка при разборе строки {0}\r\n{1}", "dsds", exception);
			}
		}
	}
}