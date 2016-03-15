using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Xls
{
	[TestFixture]
	public class PharmChemComplect11261ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("11261.xls");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("01.01.2016")));

			Assert.That(document.Lines.Count, Is.EqualTo(2));
			var line = document.Lines[0];

			Assert.That(line.Amount, Is.EqualTo(108900));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОГБУЗ Центр Контроля качества и сертификации лек. Средств Костромской области Испытательная контрольно-аналитическая лаборатория"));
			Assert.That(line.Certificates, Is.EqualTo("1168"));
			Assert.That(line.CertificatesDate, Is.EqualTo("30.10.2015"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(DateTime.Parse("01.07.2019")));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(9900));
			Assert.That(line.Period, Is.EqualTo("01.04.2020"));
			Assert.That(line.Product, Is.EqualTo("Магния сульфат д/ин."));
			Assert.That(line.Quantity, Is.EqualTo(50));
			Assert.That(line.SerialNumber, Is.EqualTo("73042015"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(1980));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}
	}
}
