using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BaltimorYoshkarOlaParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(BaltimorYoshkarOlaParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\00033318.dbf")));
			var doc = WaybillParser.Parse("00033318.dbf");
			var line = doc.Lines[0];
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ЧБ000033318"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("29.08.2011"));
			Assert.That(line.Code, Is.EqualTo("36869"));
			Assert.That(line.Product, Is.EqualTo("Окситоцин МЭЗ р-р д/ин. 5МЕ амп. 1мл. №10"));
			Assert.That(line.Producer, Is.EqualTo("Московский эндокринный з-д"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(doc.Lines[2].BillOfEntryNumber, Is.EqualTo("10130130/200611/0012231/1"));
			Assert.That(line.SerialNumber, Is.EqualTo("70411"));
			Assert.That(line.Period, Is.EqualTo("01.05.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ01.Д42360"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(doc.Lines[1].SupplierPriceMarkup, Is.EqualTo(12.57));
			Assert.That(line.SupplierCost, Is.EqualTo(22.69));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(20.63));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(4.13));
			Assert.That(line.Amount, Is.EqualTo(45.38));
			Assert.That(line.EAN13, Is.EqualTo("4602676001761"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(21.45));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(21.45));
		}
	}
}