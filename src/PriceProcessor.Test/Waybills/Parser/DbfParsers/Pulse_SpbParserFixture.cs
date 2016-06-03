using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Pulse_SpbParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("P_86001.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00086001"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("12.10.2010"));
			var line = doc.Lines[0];
			//Assert.That(.S, Is.EqualTo("815575"));
			Assert.That(line.Code, Is.EqualTo("05244"));
			Assert.That(line.Product, Is.EqualTo("Бифидумбактерин сухой фл. 5 доз х10"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(40.18));
			Assert.That(line.SupplierCost, Is.EqualTo(47.18));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(42.89));
			Assert.That(line.RegistryCost, Is.EqualTo(36.53));
			Assert.That(line.Period, Is.EqualTo("01.08.2011"));
			Assert.That(line.SerialNumber, Is.EqualTo("531"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Экополис"));
			Assert.That(line.Certificates, Is.EqualTo("СП № 002734"));
			Assert.That(line.VitallyImportant, Is.True);
		}

		/// <summary>
		/// Новые данные от задачи
		/// http://redmine.analit.net/issues/28233
		/// </summary>
		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\8352663.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(9));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("8352663"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("09.10.2014")));

			var line = document.Lines[2];
			Assert.That(line.Code, Is.EqualTo("362694"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130030/030714/0003533/3"));
			Assert.That(line.EAN13, Is.EqualTo("4603149000106"));
			Assert.That(line.CountryCode, Is.EqualTo("756"));
			Assert.That(line.UnitCode, Is.EqualTo("796"));
		}
	}
}