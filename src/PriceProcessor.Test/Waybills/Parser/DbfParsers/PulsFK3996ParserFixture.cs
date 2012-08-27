using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PulsFK3996ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00627149.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00627149"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("13.08.2012"));
			Assert.That(doc.Invoice.ShipperInfo, Is.EqualTo("ООО ФК ПУЛЬС"));
			var line = doc.Lines[0];

			Assert.That(line.Code, Is.EqualTo("05583"));
			Assert.That(line.Product, Is.EqualTo("Ампициллина т/г табл. 250 мг х20"));
			Assert.That(line.SerialNumber, Is.EqualTo("430612"));
			Assert.That(line.Period, Is.EqualTo("01.07.2015"));
			Assert.That(line.SupplierCost, Is.EqualTo(9.46));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BY.ФМ05.Д87322"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО \"ОЦС\" г. Екатеринбург"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(14.23));
			Assert.That(line.Producer, Is.EqualTo("Белмедпрепараты"));
			Assert.That(line.Country, Is.EqualTo("БЕЛАРУСЬ"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(8.6));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(8.8));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.ProducerCost, Is.EqualTo(9.68));
			Assert.That(line.EAN13, Is.EqualTo("4810133000169"));
			Assert.That(line.Amount, Is.EqualTo(94.6));
			Assert.That(line.NdsAmount, Is.EqualTo(8.6));
		}
	}
}
