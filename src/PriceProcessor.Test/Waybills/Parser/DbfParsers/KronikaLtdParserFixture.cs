using System;
using System.Data;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KronikaLtdParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("Н4375_5061.dbf", new DocumentReceiveLog(new Supplier { Id = 7524 }, new Address(new Client())));
			KronikaLtdParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\Н4375_5061.dbf"));
			Assert.That(document.Lines.Count, Is.EqualTo(24));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("061216"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("19.03.2015")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("523203"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Амбробене сироп 15мг/5мл 100мл"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Меркле ГмбХ"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(86.87m));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(96.42m));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС DE.ФМ08.Д28058"));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(106.06));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(10.99));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(28.93));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(318.19m));
		}
	}
}