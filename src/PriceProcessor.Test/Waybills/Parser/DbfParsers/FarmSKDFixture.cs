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
	public class FarmSKDFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(FarmSKDParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\30529180.DBF")));
			var document = WaybillParser.Parse("30529180.DBF");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("29180"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("05.03.2012"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2012-0011610"));
			Assert.That(line.Product, Is.EqualTo("Аллохол табл.п.о. N 50 (48)"));
			Assert.That(line.Country, Is.EqualTo("БЕЛАРУСЬ"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(36.27));
			Assert.That(line.ProducerCost, Is.EqualTo(39.9));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(42.93));
			Assert.That(line.SupplierCost, Is.EqualTo(47.22));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Amount, Is.EqualTo(472.23));
			Assert.That(line.NdsAmount, Is.EqualTo(42.93));
			Assert.That(line.Producer, Is.EqualTo("Белмедпрепараты"));
			Assert.That(line.Period, Is.EqualTo("01.02.2016"));
			Assert.That(line.SerialNumber, Is.EqualTo("030112"));
			Assert.That(line.Unit, Is.EqualTo("уп."));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BY.ФМ05.Д97735"));
			Assert.That(line.CertificatesDate, Is.EqualTo("06.02.2012"));
			Assert.That(line.EAN13, Is.EqualTo(4810133003801));
			Assert.IsNull(line.BillOfEntryNumber);
		}
	}
}