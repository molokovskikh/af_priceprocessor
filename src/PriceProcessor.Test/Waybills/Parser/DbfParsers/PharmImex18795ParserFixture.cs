using System;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PharmImex18795ParserFixture
	{
		/// <summary>
		/// Для задачи http://redmine.analit.net/issues/52502
		/// </summary>
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 18795 } }; // код поставщика Фармимэкс
			Assert.IsTrue(new WaybillFormatDetector().DetectParser(@"..\..\Data\Waybills\0004174.DBF", documentLog) is PharmImex18795Parser);
			var doc = WaybillParser.Parse(@"0004174.DBF", documentLog);
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("2/000004174"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("03.08.2016"));

			var invoice = doc.Invoice;
			Assert.That(invoice.BuyerName, Is.EqualTo("ИП Ярцева Л. В."));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("ПАО \"Фармимэкс\" Калининградски"));
			Assert.That(invoice.Amount, Is.EqualTo(2914.7));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2407"));
			Assert.That(line.Product, Is.EqualTo("Квинакс 0,015% глазные капли 15мл"));
			Assert.That(line.Producer, Is.EqualTo("АЛКОН-КУВРЕР"));
			Assert.That(line.Country, Is.EqualTo("БЕЛЬГИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SerialNumber, Is.EqualTo("15B10QA"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС BE.ФМ08.Д55586 20.03.2015"));
			Assert.That(line.SupplierCost, Is.EqualTo(340.7));
			Assert.That(line.Period, Is.EqualTo("10.02.2017"));
			Assert.That(line.Amount, Is.EqualTo(1022.1));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.RegistryDate, Is.Null);
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.NdsAmount, Is.EqualTo(92.92));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130032/200315/0002003/2"));
			Assert.That(line.EAN13, Is.EqualTo("5413895001918"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО\"Окр.центр контроля кач-ва\""));

			var s = 0m;
			foreach (var l in doc.Lines) {
				var sl = l.Quantity.Value * l.SupplierCost.Value;
				Assert.That(l.Amount, Is.EqualTo(sl));
				s += sl;
			}
			Assert.That(invoice.Amount, Is.EqualTo(s));
		}
	}
}