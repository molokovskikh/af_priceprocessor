using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class BioFarmVolgaParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(BioFarmVolgaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\260254.dbf")));

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\260254.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-ЙО00000260254"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("31.08.2011"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("М13845"));
			Assert.That(line.Product, Is.EqualTo("Валосердин 25мл"));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(24.20));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(22.77));
			Assert.That(line.SupplierCost, Is.EqualTo(25.05));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(22.77));
			Assert.That(line.Amount, Is.EqualTo(250.50));
			Assert.That(line.SerialNumber, Is.EqualTo("210311"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.RU.ФМ01.Д07977"));
			Assert.That(line.CertificatesDate, Is.EqualTo("01.03.2013"));
			Assert.That(line.Producer, Is.EqualTo("Московская фарм. ф-к"));
			Assert.That(line.Period, Is.EqualTo("01.03.2013"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);
		}
	}
}
