using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class KatrenVrnParserFixture
	{
		[Test]
		public void Parse_Katren_Vrn_LipetskFarmazia()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\264002.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(13));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("264002"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.12.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("2156221"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("SENI LADY ПРОКЛАДКИ УРОЛОГ NORMAL N20"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("ТОРУНСКИЙ З-Д ПЕРЕВЯЗОЧНЫХ МАТЕРИАЛОВ"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("польша"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(83.20));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(91.52));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(83.20));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("102010"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.09.2013"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС PL.ИМ09.В02637"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[3].RegistryCost, Is.EqualTo(190.94));

			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[3].VitallyImportant, Is.True);
		}

		[Test]
		public void Parse_Katren_LipetskFarmazia()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\6155143_Катрен(1849).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(24));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("1849"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("05.01.2011")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("15736"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АКТОВЕГИН 0,04/МЛ 5МЛ N5 АМП Р-Р Д/ИН"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Никомед Австрия ГмбХ"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("австрия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(368.70));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(405.57));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(368.7));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("10576709"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.07.2015"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС AT.ФМ08.Д30187"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);

			var doc1 = WaybillParser.Parse(@"..\..\Data\Waybills\6152807_Катрен(1759).txt");
			Assert.That(doc1.Lines.Count, Is.EqualTo(11));
			Assert.That(doc1.Lines[0].Period, Is.EqualTo("01.06.2015"));
			Assert.That(doc1.Lines[0].SerialNumber, Is.EqualTo("062010"));
		}
	}
}