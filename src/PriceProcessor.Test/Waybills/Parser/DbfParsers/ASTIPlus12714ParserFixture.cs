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
	public class ASTIPlus12714ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(ASTIFarmacevtika12799Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\70713.dbf")));
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\70713.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(15));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("   70713"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("09.07.2012"));
			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.BuyerAddress, Is.EqualTo("400038 г.Волгоград р.п.Горьковский ул.Волгоградская,7"));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6890"));
			Assert.That(line.Product, Is.EqualTo("Алка - Зельтцер таб шипучие №10"));
			Assert.That(line.Producer, Is.EqualTo("Байер"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Unit, Is.EqualTo("уп."));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(133.65455));
			Assert.That(line.SupplierCost, Is.EqualTo(147.02));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(128.84));
			Assert.That(line.ProducerCost, Is.EqualTo(141.72));
			Assert.That(line.Nds, Is.EqualTo(10));

			Assert.That(line.NdsAmount, Is.EqualTo(13.37));
			Assert.That(line.Amount, Is.EqualTo(147.02));
			Assert.That(line.Period, Is.EqualTo("16.08.2014"));
			Assert.That(line.SerialNumber, Is.EqualTo("BTAB330"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130110/061011/0007044/5"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ11.Д20016"));
			Assert.That(line.CertificatesDate, Is.EqualTo("26.09.2011"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО\"Формат качества\""));

			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.EAN13, Is.EqualTo("4008500120057"));

			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(3.60));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
		}
	}
}
