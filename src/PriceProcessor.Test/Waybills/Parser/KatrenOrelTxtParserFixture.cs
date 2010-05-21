using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class KatrenOrelTxtParserFixture
	{
		[Test]
		public void Parse_Katren_Orel()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\82936.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(38));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("82936"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("30.04.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("13509328"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АКРИДИЛОЛ 0,0125 N30 ТАБЛ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Акрихин ХФК ОАО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(203.74));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(204.40));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(185.82));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("10210"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.11.2013"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д68638"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(203.74));
			
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void Parse_Katren_Voronezh()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3767013_Катрен(91136).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(5));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("91136"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("03.05.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("24648935"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ТЕСТ-ПОЛОСКА САТЕЛЛИТ ПЛЮС ПКГЭ-02.4 N50"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("КОМПАНИЯ ЭЛТА, ООО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(290.00));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(295.47));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(295.47));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(0));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("083"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.05.2011"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("022.а2006/5582-06"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("РОСС HU.ПК12.В06358"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3633567_0_17202011.xml"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3700197_Протек-21(9041050-001).sst"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\0000470553.dbf"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3725138_Риа-Панда(07054623).TXT"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\8916.dbf"));
			Assert.IsFalse(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\890579.dbf"));
			Assert.IsTrue(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\82936.txt"));
			Assert.IsTrue(KatrenOrelTxtParser.CheckFileFormat(@"..\..\Data\Waybills\3767013_Катрен(91136).txt"));
		}
	}
}
