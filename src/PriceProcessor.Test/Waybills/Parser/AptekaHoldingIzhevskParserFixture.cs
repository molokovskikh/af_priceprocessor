using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class AptekaHoldingIzhevskParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\396559.DBF");

			Assert.That(document.Lines.Count, Is.EqualTo(5));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("396559"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("29/04/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("27233"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Депантол суппоз. вагин. N10 Россия"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Нижфарм ОАО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(202.12));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(202.12));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("200909"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д80550"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.10.2011"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[2].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00000));
			Assert.That(document.Lines[2].RegistryCost, Is.EqualTo(90.92));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(222.332));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\0000470553.dbf")));
			Assert.IsFalse(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\R1362131.DBF")));
			Assert.IsTrue(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\396559.DBF")));
			Assert.IsFalse(AptekaHoldingIzhevskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\3889082_Протек-28(212305_140089_9694708_001).dbf")));
		}
	}
}
