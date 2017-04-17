using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class VegaParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("Р-147253.dbf");
			Assert.IsTrue(VegaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\Р-147253.dbf")));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("147253"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("14.10.2014")));

			Assert.That(document.Lines.Count, Is.EqualTo(6));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("3289"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCost, Is.EqualTo(72.33));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.NdsAmount, Is.EqualTo(22.07));
			Assert.That(line.Amount, Is.EqualTo(144.66));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(61.29));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(11));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(55.22));
			Assert.That(line.Period, Is.EqualTo("05.06.2016"));
			Assert.That(line.SerialNumber, Is.EqualTo("050614"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АЮ18.Д02268"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("Сергиево-Посадский ЦСМС"));
			Assert.That(line.Product, Is.EqualTo("Бальзам СПАСАТЕЛЬ от ран и ожогов 30г"));
			Assert.That(line.Producer, Is.EqualTo("Люми ООО"));
			Assert.That(line.EAN13, Is.EqualTo(4607004431050));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.Null);
		}
	}
}
