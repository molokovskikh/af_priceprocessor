using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Xls
{
	[TestFixture]
	public class AVIA_Kos7895ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("КА62689.xls");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("КА/62689"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("14.09.2012")));

			Assert.That(document.Lines.Count, Is.EqualTo(48));
			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Джонсон Салф б/отдушки 64шт"));
			Assert.That(line.Code, Is.EqualTo("6296704/2/1"));
			Assert.That(line.EAN13, Is.EqualTo(3574660505832));
			Assert.That(line.Unit, Is.EqualTo("шт."));
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.SupplierCost, Is.EqualTo(85.97));
			Assert.That(line.Amount, Is.EqualTo(85.97));
			Assert.That(line.SupplierCostWithoutNDS, Is.Null);
			Assert.That(line.Nds, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCostWithoutNDS, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("---"));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);
		}
	}
}
