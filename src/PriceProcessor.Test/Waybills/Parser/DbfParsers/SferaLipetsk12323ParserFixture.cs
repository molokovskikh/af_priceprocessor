using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SferaLipetsk12323ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(SferaLipetsk12323Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\rn055723.DBF")));
			var doc = WaybillParser.Parse("rn055723.DBF");
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("48196"));
			Assert.That(line.Product, Is.EqualTo("Аспиратор назальный[19207]"));
			Assert.That(line.SerialNumber, Is.EqualTo("19207"));
			Assert.That(line.Period, Is.EqualTo("01.02.2013"));
			Assert.That(line.SupplierCost, Is.EqualTo(85.01));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo("РОСС CN. АЯ58.Н06475"));
			Assert.That(line.CertificatesDate, Is.EqualTo("21.12.2011"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОС \"Центр\"СКС\"\""));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(100));
			Assert.That(line.Producer, Is.EqualTo("МИР ДЕТСТВА"));
			Assert.That(line.Country, Is.EqualTo("Китай"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(77.28));
			Assert.That(line.ProducerCost, Is.EqualTo(100));
		}
	}
}