using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class OriolaStavropol13525ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Oriola_stavropol_42525_ОООБИОС_247263.dbf");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("247263"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("19.10.2012")));
			Assert.That(doc.Invoice.BuyerName, Is.EqualTo("ООО \"БИОС\""));
			Assert.That(doc.Invoice.BuyerAddress, Is.EqualTo("358000,Р-ка Калмыкия,г.Элиста,1 микрорайон,д.20Б"));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("73381"));
			Assert.That(line.Product, Is.EqualTo("Эссенциале форте Н капс. 300мг №100"));
			Assert.That(line.Producer, Is.EqualTo("А.Наттерман энд Сие."));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(1087.6));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10113080/270712/00145451"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(108.76));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(1087.6));
			Assert.That(line.SupplierCost, Is.EqualTo(1196.36));
			Assert.That(line.Amount, Is.EqualTo(1196.36));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SerialNumber, Is.EqualTo("26261"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.DE.ФМ08.Д02968"));
			Assert.That(line.Period, Is.EqualTo("01.05.2015"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.EAN13, Is.EqualTo("3582910037767"));
		}
	}
}
