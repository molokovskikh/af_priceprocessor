using Inforoom.PriceProcessor.Waybills;
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
			Assert.That(document.Lines[0].Product, Is.EqualTo("Бутылка д/дет питания БСДМ -200 шт. N1"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Туймазыстекло "));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(140));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(7.00));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(4.74));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(18));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(5.93));
		}

		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3657940_ОАС(120591).pd");
			Assert.That(document.Lines.Count, Is.EqualTo(6));

			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("02.04.2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("61604"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Глюкоза-Э р-р д/инфуз 5% 200мл фл N1x1 Эском НПК РОС"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Эском НПК"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(28));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(20.66));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(15.03));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(18.78));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(21.43));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("270210^РОСС RU.ФМ01.Д74804^01.02.2012 ФГУ \"ЦЭККМП\" Росздравнадзор270210^31.03.2010 74-2424660"));

			Assert.That(document.Lines[0].Period, Is.Null);
			Assert.That(document.Lines[0].SerialNumber, Is.Null);			
		}
	}
}