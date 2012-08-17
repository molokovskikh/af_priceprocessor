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
	public class SiaAstrahanFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(SiaAstrahanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\Р-786953.DBF")));
			var document = WaybillParser.Parse("Р-786953.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(36));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-786953"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("12.03.2012"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("24378"));
			Assert.That(line.Product, Is.EqualTo("Аква Марис капли назальные д/детей 10мл Фл-капельница Б"));
			Assert.That(line.Producer, Is.EqualTo("Ядран Галенский Лабораторий АО"));
			Assert.That(line.Country, Is.EqualTo("ХОРВАТИЯ"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.IsNull(line.ProducerCost);
			Assert.IsNull(line.ProducerCostWithoutNDS);
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(73.22));
			Assert.That(line.SupplierCost, Is.EqualTo(80.54));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(161.08));
			Assert.That(line.NdsAmount, Is.EqualTo(14.64));
			Assert.That(line.Period, Is.EqualTo("01.08.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("2041"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС HR.ФМ01.Д23988"));
			Assert.That(line.CertificatesDate, Is.EqualTo("25.10.2011"));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.EAN13, Is.EqualTo("3858881054738"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130030/251011/0004515/1"));
		}
	}
}