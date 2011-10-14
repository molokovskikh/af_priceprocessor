using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class RostaOmskParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"3907166_Роста(58464).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(8));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("58464/3"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("03.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("10137"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Дикло-Ф гл.кап.фл.0,1%  5мл"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Promed Exports Pvt. Ltd. - Индия"));
			Assert.That(doc.Lines[0].Country, Is.Null);
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(102.63));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(124.44));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(113.13));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("N10026"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.02.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС IN ФМ08 Д96178"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(103.04));

			Assert.That(doc.Lines[0].VitallyImportant, Is.True);
			Assert.That(doc.Lines[1].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(10.23));
		}
	}
}
