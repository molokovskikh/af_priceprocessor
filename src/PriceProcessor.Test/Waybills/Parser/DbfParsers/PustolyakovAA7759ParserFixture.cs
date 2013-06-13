using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PustolyakovAA7759ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\00015222.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(96));

			Assert.That(document.ProviderDocumentId, Is.EqualTo("А0000015222"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("14.03.2012"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.BuyerAddress, Is.EqualTo("425120, Марий Эл Респ, Моркинский р-н, Морки пгт, Мира ул, дом № 20, корпус а"));

			var line = document.Lines[0];

			Assert.That(line.Code, Is.EqualTo("Н0000015548"));
			Assert.That(line.Product, Is.EqualTo("27 Oral-B Classic 40 зубн. щетка medium"));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.SerialNumber, Is.EqualTo(null));
			Assert.That(line.SupplierCost, Is.EqualTo(17.70));
			Assert.That(line.Amount, Is.EqualTo(17.70));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.EAN13, Is.EqualTo("3014260275921"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo(null));
			Assert.That(line.Country, Is.EqualTo("ИНДИЯ"));
			Assert.That(line.Producer, Is.EqualTo("Procter & Gamble"));
			Assert.That(line.Certificates, Is.EqualTo("РОСC IE.АИ35.В06070"));
			Assert.That(line.CertificatesDate, Is.EqualTo(null));
		}


		[Test]
		public void Check_file_Format()
		{
			Assert.IsTrue(PustolyakovAA7759Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\00015222.dbf")));
		}
	}
}