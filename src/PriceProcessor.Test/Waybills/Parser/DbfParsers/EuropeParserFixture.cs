using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class EuropeParserFixture
	{
		/// <summary>
		/// Для задачи http://redmine.analit.net/issues/39085
		/// </summary>
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("европа-В0000004236.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Е0000004236"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("04.09.2015"));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Нарзан 1л газ (6) пэт"));
			Assert.That(line.Code, Is.Null);
			Assert.That(line.Unit, Is.EqualTo("бут."));
			Assert.That(line.Quantity, Is.EqualTo(6));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(54.8200));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.NdsAmount, Is.EqualTo(50.1700));
			Assert.That(line.Amount, Is.EqualTo(328.9200));
			Assert.That(line.Certificates, Is.EqualTo(null));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(null));
			Assert.That(line.CertificatesDate, Is.EqualTo(null));
			Assert.That(line.CertificateAuthority, Is.EqualTo(null));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo(null));
			Assert.That(line.Country, Is.EqualTo(null));
			Assert.That(line.EAN13, Is.EqualTo(null));
			Assert.That(line.Producer, Is.EqualTo(null));
		}
    }
}