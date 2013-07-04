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
	public class AptekaHoldingSPBParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(AptekaHoldingSPBParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\NKL_1892886.dbf")));
			var document = WaybillParser.Parse(@"C:\00141601 (1).dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-1892886/0"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("01.12.2011"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("8667"));
			Assert.That(line.Product, Is.EqualTo("Анаферон детск. табл. д/рассасыв. N20 Россия"));

			Assert.That(line.ProducerCost, Is.EqualTo(103.2));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(102.95));

			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(5));

			Assert.That(line.Amount, Is.EqualTo(566.25));
			Assert.That(line.NdsAmount, Is.EqualTo(51.50));

			Assert.That(line.Producer, Is.EqualTo("Материа Медика ПФ ЗАО"));
			Assert.That(line.Country, Is.EqualTo("Россия"));

			Assert.That(line.Period, Is.EqualTo("01.09.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д99156"));
			Assert.That(line.CertificatesDate, Is.EqualTo("22.08.2011"));

			Assert.That(line.EAN13, Is.EqualTo("4607009581071"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo(null));

			Assert.That(line.SerialNumber, Is.EqualTo("5030811"));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
		}
	}
}