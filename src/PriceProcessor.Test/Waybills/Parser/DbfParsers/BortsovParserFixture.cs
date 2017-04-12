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
	class BortsovParserFixture
	{
		/// <summary>
		/// К задаче 
		/// http://redmine.analit.net/issues/29249
		/// Новый парсер не нужен, так как в процессе задачи поставщик исправил косяки в формате
		/// Тест оставим на всякий случай
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\0060956.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(2));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("СБЛ0060956"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("29.10.2014")));

			var line = document.Lines.First();
			Assert.That(line.Product, Is.EqualTo("Фрутоняня ф.п. 90г Витам.салат Яблоко/шиповник/клю"));
			Assert.That(line.Certificates, Is.EqualTo("7628627"));
			Assert.That(line.CertificatesDate, Is.EqualTo("01.02.2016"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(26.57));
			Assert.That(line.Nds, Is.EqualTo(10)); 
			Assert.That(line.SupplierCost, Is.EqualTo(29.23));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОС \"Липецкий центр мониторинга и менеджмента\""));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.NdsAmount, Is.EqualTo(7.97));
			Assert.That(line.Amount, Is.EqualTo(87.69));
			Assert.That(line.EAN13, Is.EqualTo(4600338006185));

			Assert.That(document.Invoice.BuyerName, Is.EqualTo("ООО  \"Медфарм \""));
			Assert.That(document.Invoice.BuyerAddress, Is.EqualTo(",359050,Калмыкия Респ,,Городовиковск г,,Советская"));
			Assert.That(document.Invoice.BuyerINN, Is.EqualTo("801005219"));
			Assert.That(document.Invoice.BuyerKPP, Is.EqualTo("80101001"));
			Assert.That(document.Invoice.SellerName, Is.EqualTo("ИП Борцова Людмила Дмитриевна"));
			Assert.That(document.Invoice.SellerAddress, Is.EqualTo("355029, РФ, Ставропольский край, г. Ставрополь, ул"));
			Assert.That(document.Invoice.SellerINN, Is.EqualTo("263500323873"));
			Assert.That(document.Invoice.SellerKPP, Is.EqualTo("0"));
			Assert.That(document.Invoice.Amount, Is.EqualTo(87.69));
			Assert.That(document.Invoice.AmountWithoutNDS, Is.EqualTo(79.72));
			Assert.That(document.Invoice.NDSAmount, Is.EqualTo(7.97));
		}
	}
}
