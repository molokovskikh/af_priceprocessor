using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class VectorGroupKalugaParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("a2182.txt");

			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("2182"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("10.06.2011"));

			Assert.That(doc.Lines[0].Code, Is.Null);
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Клинекс плат.Велти.персик"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("КИМБЕРЛИ-КЛАРК гигиена"));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(4.47));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(5.28));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.Null);
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(18));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(4.03));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(26.40));
			Assert.That(doc.Lines[0].SerialNumber, Is.Null);
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС IT АЕ95 В 00535"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Польша"));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("17.01.2009"));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].Unit, Is.EqualTo("шт."));
			Assert.That(doc.Lines[0].ExciseTax, Is.Null);
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo("10127020/130808/0008596/01"));
			Assert.That(doc.Lines[0].EAN13, Is.EqualTo("9000100398237"));

			var invoice = doc.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("2182"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("10.06.2011"));
			Assert.That(invoice.SellerName, Is.EqualTo("Общество с ограниченной ответственностью \"ВЕКТОР ГРУПП\""));
			Assert.That(invoice.SellerAddress, Is.EqualTo("248003, г.Калуга, ул.Болдина, д.67, стр.20"));
			Assert.That(invoice.SellerINN, Is.EqualTo("4027085792"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("402945001"));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("ООО \"ВЕКТОР ГРУПП\", 248009, г.Калуга, ул.Грабцевское шоссе, д.33"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("Общество с ограниченной ответственностью \"Елена\", г.Калуга, ул.Ленина, 47"));
			Assert.That(invoice.PaymentDocumentInfo, Is.Null);
			Assert.That(invoice.BuyerName, Is.EqualTo("Общество с ограниченной ответственностью \"Елена\""));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("г.Калуга, ул.Воронина, 25"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("4026009069"));
			Assert.That(invoice.BuyerKPP, Is.EqualTo("402801001"));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(0.00));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(0.00));
			Assert.That(invoice.Amount10, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(312.46));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(56.24));
			Assert.That(invoice.Amount18, Is.EqualTo(368.70));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(312.46));
			Assert.That(invoice.NDSAmount, Is.EqualTo(56.24));
			Assert.That(invoice.Amount, Is.EqualTo(368.70));
		}

		[Test]
		public void Parse2()
		{
			var doc = WaybillParser.Parse("a34202.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(6));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("34202"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("22.06.2011"));

			Assert.That(doc.Lines[0].Code, Is.Null);
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Крем ВИНОГРАД увлажняющий 40мл НК нор/комб/к*36"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("НЕВСКАЯ КОСМЕТИКА"));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(18.53));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(21.86));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.Null);
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(18));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(10.00));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(65.58));
			Assert.That(doc.Lines[0].SerialNumber, Is.Null);
			Assert.That(doc.Lines[0].Period, Is.EqualTo("28.10.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU АЕ45 В 27744"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("14.12.2012"));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].Unit, Is.EqualTo("шт."));
			Assert.That(doc.Lines[0].ExciseTax, Is.Null);
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo("-"));
			Assert.That(doc.Lines[0].EAN13, Is.EqualTo("4600697407807"));

			var invoice = doc.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("34202"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("22.06.2011"));
			Assert.That(invoice.SellerName, Is.EqualTo("Общество с ограниченной ответственностью Компания \"Вектор\""));
			Assert.That(invoice.SellerAddress, Is.EqualTo("248017, г.Калуга, ул.Параллельная, д.11, стр.18"));
			Assert.That(invoice.SellerINN, Is.EqualTo("4028030080"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("482543001"));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("Липецкий филиал ООО Компания \"Вектор\", 398042, г.Липецк, Поперечный проезд, д.1А"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("Областное государственное унитарное предприятие \"Липецкфармация\" св. о рег.№1024840823688, с.Хлевное, ул.Свободы, д.51"));
			Assert.That(invoice.PaymentDocumentInfo, Is.Null);
			Assert.That(invoice.BuyerName, Is.EqualTo("Областное государственное унитарное предприятие \"Липецкфармация\" св. о рег.№1024840823688"));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("г.Липецк,ул.Гагарина, д.113"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("4826022196"));
			Assert.That(invoice.BuyerKPP, Is.EqualTo("482601001"));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(451.14));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(45.11));
			Assert.That(invoice.Amount10, Is.EqualTo(496.25));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(482.78));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(86.90));
			Assert.That(invoice.Amount18, Is.EqualTo(569.68));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(933.92));
			Assert.That(invoice.NDSAmount, Is.EqualTo(132.01));
			Assert.That(invoice.Amount, Is.EqualTo(1065.93));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(VectorGroupKalugaParser.CheckFileFormat(@"..\..\Data\Waybills\a2182.txt"));
			Assert.IsTrue(VectorGroupKalugaParser.CheckFileFormat(@"..\..\Data\Waybills\a34202.txt"));
		}
	}
}