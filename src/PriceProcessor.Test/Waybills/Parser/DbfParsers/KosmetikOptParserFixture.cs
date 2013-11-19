﻿using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KosmetikOptParserFixture
	{
		[Test]
		public void Parser()
		{
			var doc = WaybillParser.Parse(@"real_№УТKO0000566_from_16.03.2011.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(17));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("УТKO0000566"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("16.03.2011"));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("УТ000000208"));
			Assert.That(line.Product, Is.EqualTo("Сплат Professional УЛЬТРАКОМПЛЕКС, 100мл, зубная паста (арт.У-115)"));
			Assert.That(line.Producer, Is.EqualTo("ООО \"СПЛАТ-КОСМЕТИКА\""));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.SupplierCost, Is.EqualTo(61.55));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(52.16));
			Assert.That(line.Amount, Is.EqualTo(123.1));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.ProducerCostWithoutNDS, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.NdsAmount, Is.EqualTo(18.78));
		}

		[Test]
		public void KosmetikPlusParser()
		{
			var doc = WaybillParser.Parse(@"real_No.0001684_from_05.07.2011.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("УТKO0001684"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("05.07.2011"));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("УТ000000582"));
			Assert.That(line.Product, Is.EqualTo("20610 ЗЩ Колгейт Классическая Чистота жест."));
			Assert.That(line.Producer, Is.EqualTo("\"Colgate-Palmolive\""));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.SupplierCost, Is.EqualTo(11.12));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(9.42));
			Assert.That(line.Amount, Is.EqualTo(55.60));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.ProducerCostWithoutNDS, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.NdsAmount, Is.EqualTo(8.50));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(KosmetikOptParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\real_№УТKO0000566_from_16.03.2011.dbf")));
			Assert.IsTrue(KosmetikOptParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\real_No.0001684_from_05.07.2011.dbf")));
		}

		[Test]
		public void Kosmetic_plus_parser2()
		{
			var doc = WaybillParser.Parse("real №УТKП0005371 from 14.11.2013-1.dbf");
			Assert.AreEqual(78, doc.Lines.Count);
			Assert.AreEqual("ООО \"Инфанта\", г. Казань, ул. Гвардейская, д. № 42", doc.Invoice.RecipientAddress);
			var line = doc.Lines[0];
			Assert.AreEqual("2258 ЛЕСНОЙ БАЛЬЗАМ Ополаскиватель" +
				" с экстратами коры и дуба и пихты на отваре трав 250мл", line.Product);
			Assert.AreEqual(0, line.Nds);
		}
	}
}