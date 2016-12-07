using System;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class GrandCapitalVlg19513ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("4-002126_38015.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("16-0-38015"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("06/12/2016")));
			Assert.That(doc.Lines.Count, Is.EqualTo(11));

			var invoice = doc.Invoice;
			Assert.That(invoice.SellerName, Is.EqualTo("ООО ФК Гранд Капитал Волгоград"));
			Assert.That(invoice.Amount, Is.EqualTo(5585.53));
			Assert.That(invoice.NDSAmount, Is.EqualTo(507.79));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(507.79));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(0));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(5077.74));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2-001488"));
			Assert.That(line.EAN13, Is.EqualTo("4607045190268"));
			Assert.That(line.SupplierCost, Is.EqualTo(175.22));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(159.29));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SerialNumber, Is.EqualTo("TD0128A"));
			Assert.That(line.Period, Is.EqualTo("01.02.2019"));
			Assert.That(line.DateOfManufacture, Is.EqualTo(DateTime.Parse("01/03/2016")));
			Assert.That(line.Product, Is.EqualTo("Вольтарен эмульгель гель д/нар.прим. 1% 20г"));
			Assert.That(line.Country, Is.EqualTo("Швейцария"));
			Assert.That(line.Producer, Is.EqualTo("Новартис Консьюмер Хелс С.А."));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130060/120716/0010323/02"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС СН.ФМ09.Д98548"));
			Assert.That(line.CertificatesDate, Is.EqualTo("28.02.2019"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО\"Институт фармацевтической биотехнологии\""));
			Assert.That(line.Amount, Is.EqualTo(525.66));
			Assert.That(line.NdsAmount, Is.EqualTo(47.79));
			Assert.That(line.OrderId, Is.EqualTo(91640569));
		}
	}
}
