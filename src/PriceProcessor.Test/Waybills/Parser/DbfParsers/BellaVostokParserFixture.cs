using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class BellaVostokParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/55681
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("белла-восток-F335064.DBF");
			Assert.That(document.Parser, Is.EqualTo("BellaVostokParser"));
			Assert.That(document.ProviderDocumentId.Trim(), Is.EqualTo("335064"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2016, 10, 13)));

			//проверяем что для разбора данного формата файла подходит только один парсер
			var detector = new WaybillFormatDetector();
			var parsers = detector.GetSuitableParsers(@"..\..\Data\Waybills\белла-восток-F335064.DBF", null).ToList();
			Assert.That(parsers.ToList().Count, Is.EqualTo(1));

			var line0 = document.Lines[0];
			Assert.That(line0.Code, Is.EqualTo("190450"));
			Assert.That(line0.Product, Is.EqualTo("E.DEPIL Восков.полоски д/лица 20ш"));
			Assert.That(line0.EAN13, Is.EqualTo(8030009040075));
			Assert.That(line0.Quantity, Is.EqualTo(1));
			Assert.That(line0.Country, Is.EqualTo("Италия"));
			Assert.That(line0.Producer, Is.Null);
			Assert.That(line0.Period, Is.Null);
			Assert.That(line0.Nds, Is.EqualTo(18));
			Assert.That(line0.SupplierCost, Is.EqualTo(208.43)); 
			Assert.That(line0.SupplierCostWithoutNDS, Is.EqualTo(176.64));
			Assert.That(line0.VitallyImportant, Is.Null);
			Assert.That(line0.BillOfEntryNumber, Is.EqualTo("10110060/100810/0010685/0"));
			Assert.That(line0.Certificates, Is.EqualTo("РОСС IT ПК5 В36663 НИИ медицины Труда 16.12.14"));
			Assert.That(line0.CertificatesDate, Is.Null);
			Assert.That(line0.DateOfManufacture, Is.Null);
			Assert.That(line0.SerialNumber, Is.Null);

			var invoice = document.Invoice;
			Assert.That(invoice.InvoiceNumber, Is.Null);
			Assert.That(invoice.RecipientName, Is.Null);
			Assert.That(invoice.InvoiceDate, Is.Null);
		}
	}
}
