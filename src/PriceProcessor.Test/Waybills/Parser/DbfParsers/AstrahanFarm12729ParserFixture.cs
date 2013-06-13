using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class AstrahanFarm12729ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("360616.dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Ас00000360616"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("23.10.2012")));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Ревит др №100"));
			Assert.That(line.Code, Is.EqualTo("211552656"));
			Assert.That(line.Producer, Is.EqualTo("Марбиофарм ОАО"));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("770912"));
			Assert.That(line.Period, Is.EqualTo("01.10.2013"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(9.15));
			Assert.That(line.SupplierCost, Is.EqualTo(10.06));
			Assert.That(line.Amount, Is.EqualTo(100.6));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(9.15));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(8.36));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}
	}
}
