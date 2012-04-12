using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BiolainDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			//Assert.IsTrue(BiolainDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\5820.dbf")));
			var document = WaybillParser.Parse("5820.dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("5820"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("02.04.2012"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("541722"));
			Assert.That(line.Product, Is.EqualTo("Colgate 360 Sensitive Pro-Relief для чувствительных зубов~щетка зубн. ультра мягк. уп.герметич. 1~Colgate Sanxiao Китай"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(83.36));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(94.07));
			Assert.That(line.SupplierCost, Is.EqualTo(111));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Quantity, Is.EqualTo(4));
			Assert.That(line.Producer, Is.EqualTo("Colgate Sanxiao"));
			Assert.That(line.Country, Is.EqualTo("Китай"));
			Assert.That(line.Period, Is.EqualTo("01.01.2020"));
			Assert.That(line.SerialNumber, Is.EqualTo("-"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(67.73));
			Assert.That(line.Certificates, Is.EqualTo("РОСС CN ПК04 В00024"));
			Assert.That(line.VitallyImportant, Is.False);
		}
	}
}
