using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class BeautyLife18663ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("2178.DBF");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("00002178"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("15.03.2016")));
			Assert.That(document.Invoice.BuyerName, Is.EqualTo("ГУП РМЭ \"Аптека №9\" (Новый Торъял)"));
			Assert.That(document.Lines.Count, Is.EqualTo(51));

			var line = document.Lines[0];
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Code, Is.EqualTo("861823"));
			Assert.That(line.SupplierCost, Is.EqualTo(43.86));
			Assert.That(line.NdsAmount, Is.EqualTo(13.38));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.DE.ПК12.В02601 до 26.09.2016"));
			Assert.That(line.Amount, Is.EqualTo(87.72));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Product, Is.EqualTo("Натурелла прокладки ежедн. нормал 20 шт"));
			Assert.That(line.Producer, Is.EqualTo("Always"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(BeautyLife18663Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\2178.DBF")));
		}

	}
}