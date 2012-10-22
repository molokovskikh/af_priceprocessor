п»їusing Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.SstParsers;
using NUnit.Framework;
using System;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class UkonParserFixture
	{
		[Test, Description("РќР°РєР»Р°РґРЅР°СЏ СЃ Р¦РµРЅРѕР№ РїРѕСЃС‚Р°РІС‰РёРєР° СЃ РќР”РЎ СЂР°РІРЅРѕР№ 0.")]
		public void Parse_With_Zero_SupplierCost()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\7455319.sst");

			Assert.That(doc.Lines.Count, Is.EqualTo(3));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РЎРњ-7455319/00"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("23.03.2011")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("2753"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("РЎСѓРїСЂР°СЃС‚РёРЅ С‚Р°Р±Р». 25РјРі N20 Р’РµРЅРіСЂРёСЏ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Egis Pharmaceuticals Plc"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Р’РµРЅРіСЂРёСЏ"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(30));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(91.10));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(91.10));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(93.73));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(0));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(93.89));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("Р РћРЎРЎ.HU.Р¤Рњ08.Р”98806"));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(0.00));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(2733.00));

			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.08.2015"));
			//Assert.That(doc.Lines[0].ProductId, Is.Null);
			Assert.That(doc.Lines[0].ProductEntity, Is.Null);
			Assert.That(doc.Lines[0].ProducerId, Is.Null);
		}

		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\0004076.sst");
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000004076"));

			Assert.That(doc.Lines[0].Product, Is.EqualTo("РЎРѕР»РѕРґРєРѕРІРѕРіРѕ РєРѕСЂРЅСЏ СЃРёСЂРѕРї С„Р».100 Рі"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("201109^Р РћРЎРЎ RU.Р¤Рњ05.Р”11132^01.12.11201109^74-2347154^25.11.09 Р“РЈР— РћР¦РЎРљРљР› Рі. Р§РµР»СЏР±РёРЅСЃРє"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.12.11"));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("201109"));

			Assert.That(doc.Lines[1].Product, Is.EqualTo("Р­РІРєР°Р»РёРїС‚Р° РЅР°СЃС‚РѕР№РєР° С„Р».25 РјР»"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("151209^Р РћРЎРЎ Р¤Рњ05.Р”36360^01.12.14151209^74-2370989^18.01.10 Р“РЈР— РћР¦РЎРљРљР› Рі. Р§РµР»СЏР±РёРЅСЃРє"));
			Assert.That(doc.Lines[1].Period, Is.EqualTo("01.12.14"));
			Assert.That(doc.Lines[1].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.02.10")));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
		}

		[Test]
		public void Parse_without_supplier_cost_without_nds()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\8521183.sst");
		}

		[Test]
		public void Parse_with_zero_supplier_cost_without_nds()
		{
			var doc = WaybillParser.Parse(@"9907125-002.sst");

			Assert.That(doc.Lines.Count, Is.EqualTo(1));

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("9907125-002"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.06.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("1660"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Р”Р•РЎР