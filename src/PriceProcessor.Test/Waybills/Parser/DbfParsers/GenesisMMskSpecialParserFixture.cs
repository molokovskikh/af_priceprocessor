using Common.Tools;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class GenesisMMskParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("M-84275.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("М-84275"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("27.10.2011"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6606"));
			Assert.That(line.Product, Is.EqualTo("НАФТИЗИН КАП. ФЛ.-КАП. 0.1% 15МЛ"));
			Assert.That(line.Quantity, Is.EqualTo(500));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(3.71));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(185.50));
			Assert.That(line.Amount, Is.EqualTo(2040.50));
			Assert.That(line.SerialNumber, Is.EqualTo("1180911"));
			Assert.That(line.Period, Is.EqualTo("01.10.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОССRUФМ01Д26222"));
			Assert.That(line.Producer, Is.EqualTo("ДАВ ФАРМ ООО"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(3.39));
			Assert.That(line.Country, Is.EqualTo("Российская Федерация"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("-"));
			Assert.That(line.EAN13, Is.EqualTo("4612728340021"));
		}
	}
}