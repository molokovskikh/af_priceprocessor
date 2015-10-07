using System;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class MedServiceParserFixture
	{
		/// <summary>
		/// Для задачи http://redmine.analit.net/issues/39383
		/// </summary>
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 257 } }; // код поставщика МедСервис
			Assert.IsTrue(new WaybillFormatDetector().DetectParser(@"..\..\Data\Waybills\2ddd.DBF", documentLog) is MedServiceParser);
			var doc = WaybillParser.Parse(@"2ddd.DBF", documentLog);
			var line = doc.Lines[0];
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("     2"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("01.10.2015"));
			Assert.That(line.Code, Is.EqualTo("13"));
			Assert.That(line.Product, Is.EqualTo("Ингалятор компрессорный Флоренция"));
			Assert.That(line.Producer, Is.EqualTo("\"MED 2000 S.R.L.\""));
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС IT.АГ88.Д73187"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(2928.00));
			Assert.That(line.SupplierCost, Is.EqualTo(2928.00));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.Amount, Is.EqualTo(8784.00));
		}
    }
}