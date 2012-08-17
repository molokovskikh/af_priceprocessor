using System;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class GodunovDbfParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\dok.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(18));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("04050-Q3671"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("04/05/2010")));

			Assert.That(document.Lines[0].Code, Is.EqualTo("07167018"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Олазоль аэр 80г N 1"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("ЗАО \"Алтайвитамины\""));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(150.32));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(198.42));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("180210"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ10.Д73128"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.03.2012"));
			Assert.That(document.Lines[3].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[3].RegistryCost, Is.EqualTo(76.42));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(180.38));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(20));
		}

		[Test]
		public void CheckFileFormat()
		{
			Assert.IsFalse(GodunovDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416.dbf")));
			Assert.IsFalse(GodunovDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1016416_char.DBF")));
			Assert.IsFalse(GodunovDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\fm21554.dbf")));
			Assert.IsTrue(GodunovDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\dok.dbf")));
			Assert.IsFalse(GodunovDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1040150.DBF")));
			Assert.IsFalse(GodunovDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\8916.dbf")));
			Assert.IsFalse(GodunovDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\95472.dbf")));
		}
	}
}