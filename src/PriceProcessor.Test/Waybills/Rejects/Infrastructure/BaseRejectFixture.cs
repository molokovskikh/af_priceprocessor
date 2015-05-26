using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Rejects.Infrastructure
{
	[TestFixture]
	class BaseRejectFixture
	{
		/// <summary>
		/// Создает лог отказа для тестов парсеров отказов
		/// </summary>
		/// <param name="filename">Имя файла в data/rejects в котором лежит отказ</param>
		/// <returns></returns>
		public DocumentReceiveLog CreateRejectLog(string filename)
		{
			var log = new DocumentReceiveLog(new Supplier(), new Address(new Client()));
			//Имя файла должно быть задано, так как от него будет зависеть работа парсера - сам парсер не проверяет лог на то, что он отказный
			log.LocalFileName = @"..\..\data\rejects\" + filename;
			return log;
		}
	}
}
