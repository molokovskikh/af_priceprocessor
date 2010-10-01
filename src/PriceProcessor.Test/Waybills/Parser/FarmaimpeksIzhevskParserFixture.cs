using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	public class FarmaimpeksIzhevskParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\fm21554.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("21554"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("23/03/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("23159"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Конкор 5мг №30 таб.п/о/плен"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Мерк КГаА, Германия"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Германия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(159.6100));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(180.9400));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("105585"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("POCCDEФM08Д18758"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.08.2014"));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[1].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(159.6100));
			Assert.That(document.Lines[1].RegistryCost, Is.Null);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(199.0300));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.Null);
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(SIAInternationalOmskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(SIAInternationalOmskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsTrue(SIAInternationalOmskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\fm21554.dbf")));
			Assert.IsFalse(SIAInternationalOmskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(SIAInternationalOmskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(SIAInternationalOmskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\95472.dbf")));
		}
	}
}
