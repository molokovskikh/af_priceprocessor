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
	public class PrimerFarmaParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(FarmGroupParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\2505.DBF")));
			var document = WaybillParser.Parse("2505.DBF");

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("8759"));
			Assert.That(line.Product, Is.EqualTo("Ацикловир Гексал 5% крем д/наруж. прим. 2г Туба (R)"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(55.99));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(60.44));
			Assert.That(line.SupplierCost, Is.EqualTo(66.48));
			Assert.That(line.RegistryCost, Is.EqualTo(56.1));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(1));
			//Assert.That(line.Amount, Is.EqualTo(3573.57));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Producer, Is.EqualTo("Салютас Фарма ГмбХ (ГЕРМАНИЯ)"));
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Period, Is.EqualTo("01.11.2015"));
			Assert.That(line.SerialNumber, Is.EqualTo("CD9853"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д99296"));
			Assert.That(line.VitallyImportant, Is.True);
		}
	}
}