using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Rhino.Mocks;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class SiaMoscowParserFixture
	{
		/// <summary>
		/// К задаче
		/// http://redmine.analit.net/issues/30241
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("СИА-827044092_T6466561_2_0.dbf");
			Assert.That(SiaMoscowParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\СИА-827044092_T6466561_2_0.dbf")), Is.True);
			Assert.That(document.ProviderDocumentId, Is.EqualTo("T6466561/2"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2014, 12, 10)));

			var invoice = document.Invoice;
			Assert.That(invoice.RecipientName, Is.EqualTo("Консул ООО - Жуковский Клубная"));
			Assert.That(invoice.RecipientId, Is.EqualTo(827044092));
			Assert.That(invoice.BuyerId, Is.EqualTo(827044091));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("T6466561/2"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(new DateTime(2014, 12, 10)));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(7807.20));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(780.72));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(0));
			Assert.That(invoice.NDSAmount, Is.EqualTo(780.72));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(7807.20));
			Assert.That(invoice.Amount, Is.EqualTo(8587.92));


			var line = document.Lines[0];
			Assert.That(line.SerialNumber, Is.EqualTo("J65066"));
			Assert.That(line.Period, Is.EqualTo("31.05.2017"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д11811"));
			Assert.That(line.CertificatesDate, Is.EqualTo("31.07.2014"));
			Assert.That(document.Lines[3].ProducerCostWithoutNDS, Is.EqualTo(197.6));
			Assert.That(document.Lines[3].RegistryCost, Is.EqualTo(197.6));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(356));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(391.49));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.NdsAmount, Is.EqualTo(35.59));
			Assert.That(line.Amount, Is.EqualTo(391.49));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ФМ08(М)"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo("4603365000065"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130032/010814/0005051/06"));
			Assert.That(line.Producer, Is.EqualTo("Пфайзер Мэнюфэкчуринг Дойчленд"));
			Assert.That(line.Product, Is.EqualTo("Аккупро 10мг Таб. п/пл/об. Х30"));
			Assert.That(line.Code, Is.EqualTo("4732"));
			Assert.That(line.OrderId, Is.EqualTo(65140301));
		}
	}
}
