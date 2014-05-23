using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class CSMedicaKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("0000007622.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(8));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000007622"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("26.07.2011"));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("00001498"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Тон. мех. CS Healthcare CS-105 (со встр. фоненд.)"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Omron"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Китай"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(375.00));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(375.00));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(0));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(0));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(750.00));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС CN.ME20.B06811"));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("29.10.09"));
		}
	}
}