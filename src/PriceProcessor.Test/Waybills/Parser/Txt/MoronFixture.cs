using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class MoronFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"moron.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(9));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("9239945"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("6.08.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("39584"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Анальгин амп. 50% 2мл №10"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Микроген"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(28));
			//Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(29.14));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(29.14));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			//Assert.That((doc.Lines[0].));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("040410"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.04.13"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС.RU.ФМ10.Д26320"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(false));
		}

		[Test]
		public void Parse_new_moron_format()
		{
			var waybill = WaybillParser.Parse("7715039_Морон(226_Морон(660979)).txt");
			Assert.That(waybill.Lines.Count, Is.EqualTo(3));
			Assert.That(waybill.DocumentDate, Is.EqualTo(new DateTime(2011, 4, 6)));
			Assert.That(waybill.ProviderDocumentId, Is.EqualTo("660979"));
			var line = waybill.Lines[0];
			Assert.That(line.Code, Is.EqualTo("5743"));
			Assert.That(line.Product, Is.EqualTo("Нистатин таб. п/о 500000ЕД №20/пачки"));
			Assert.That(line.VitallyImportant, Is.True);
		}
	}
}