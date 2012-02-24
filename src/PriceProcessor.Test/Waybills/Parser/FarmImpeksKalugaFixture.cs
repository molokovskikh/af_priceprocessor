using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.SstParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class FarmImpeksKalugaFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(FarmImpeksKalugaParser.CheckFileFormat(@"..\..\Data\Waybills\КФ000000350.sst"));
			var doc = WaybillParser.Parse("КФ000000350.sst");
			Assert.That(doc.Lines.Count, Is.EqualTo(19));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("КФ000000350"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("20.02.2012")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("00000000182"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Андипал таб. №10"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("ДАЛЬХИМФАРМ"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Российская Федерация"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(50));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(7.64));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(7.00));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo(null));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("60211^РОСС RU.ФМ10.Д97796^28.02.2011^ООО \"Сибирский центр декларирования и сертификации\"^01.09.2013"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo(null));
			Assert.That(doc.Lines[0].EAN13, Is.EqualTo("4602824000745"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(false));
		}
	}
}
