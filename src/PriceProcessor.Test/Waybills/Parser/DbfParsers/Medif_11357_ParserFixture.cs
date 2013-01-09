using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Medif_11357_ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00008388.dbf");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("8388"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("27.12.2012")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("142247"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Азитрокс (капс. 500 мг  №3)"));
			Assert.That(doc.Lines[0].Unit, Is.EqualTo("уп."));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(218.4467));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(21.8433));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(720.87));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("3021/26,11,12"));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.Null);
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Фармстандарт-Лексредства"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.10.2015"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("190912"));
			Assert.That(doc.Lines[0].VitallyImportant, Is.True);
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(193.32));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(193.32));
		}
	}
}
