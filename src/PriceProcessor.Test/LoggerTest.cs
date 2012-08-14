using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;
using log4net;
using log4net.Config;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class LoggerTest : IntegrationFixture
	{
		[Test]
		public void DBLoggerTest()
		{
			LogManager.ResetConfiguration();
			ILog logger = LogManager.GetLogger(GetType());
			XmlConfigurator.Configure();
			logger.Error("Не удалось разобрать накладную",new Exception("Ошибка!"));
			logger.Error("Не удалось разобрать накладную", new DbfException(String.Format("Не могу преобразовать значение '{0}' к числу, строка {1} столбец {2}",
					1, 2, 3)));
			logger.Error("Не удалось разобрать накладную",new IndexOutOfRangeException("Индекс находился вне границ массива"));
			Assert.That(session.QueryOver<Log>().Where(l => l.Source == "PriceProcessor" &&
				l.Message == "Не удалось разобрать накладную").List().Count, Is.EqualTo(3));
			Assert.That(session.QueryOver<Log>().Where(l => l.Source == "FilterPriceProcessor" &&
				l.Message == "Не удалось разобрать накладную").List().Count, Is.EqualTo(1));
		}
	}
}
