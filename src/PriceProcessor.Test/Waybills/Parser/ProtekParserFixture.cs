п»їusing Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using System;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ProtekParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\1008fo.pd");
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("25.01.10")));

			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Р‘СѓС‚С‹Р»РєР° Рґ/РґРµС‚ РїРёС‚Р°РЅРёСЏ Р‘РЎР”Рњ -200 С€С‚. N1"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("РўСѓР№РјР°Р·С‹СЃС‚РµРєР»Рѕ "));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Р РѕСЃСЃРёСЏ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(140));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(7.00));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(4.74));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(18));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(5.93));
			Assert.That(document.Lines[0].SerialNumber, Is.Null);
		}

		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3657940_РћРђРЎ(120591).pd");
			Assert.That(document.Lines.Count, Is.EqualTo(6));

			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("02.04.2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("61604"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Р“Р»СЋРєРѕР·Р°-Р­ СЂ-СЂ Рґ/РёРЅС„СѓР· 5% 200РјР» С„Р» N1x1 Р­СЃРєРѕРј РќРџРљ Р РћРЎ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Р­СЃРєРѕРј РќРџРљ"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Р РѕСЃСЃРёСЏ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(28));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(20.66));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(15.03));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(18.78));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(21.43));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("270210^Р РћРЎРЎ RU.Р¤Рњ01.Р”74804^01.02.2012 Р¤Р“РЈ \"Р¦Р­РљРљРњРџ\" Р РѕСЃР·РґСЂР°РІРЅР°РґР·РѕСЂ270210^31.03.2010 74-2424660"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("270210"));

			Assert.That(document.Lines[0].Period, Is.Null);
		}
	}
}