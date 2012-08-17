using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills.Parser;
using Castle.ActiveRecord;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class TrediFarmCheboksarySpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 7999u } }; // код поставщика Тредифарм, Чебоксары			
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\TrediFarmCheboksary.dbf", documentLog) is TrediFarmCheboksarySpecialParser);

			var doc = WaybillParser.Parse("TrediFarmCheboksary.dbf", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(5));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНЧ-000000022838"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("28.03.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00010686"));
			Assert.That(line.Product, Is.EqualTo("Калия йодид 100мкг табл №100"));
			Assert.That(line.Producer, Is.EqualTo("Оболенское-фармацевтическое предпр-е"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(44.0356));
			Assert.That(line.RegistryCost, Is.EqualTo(44.0300));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(12.5000));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(49.5400));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(54.4900));
			Assert.That(line.NdsAmount, Is.EqualTo(4.9500));
			Assert.That(line.Amount, Is.EqualTo(54.4900));
			Assert.That(line.SerialNumber, Is.EqualTo("180910"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ01.Д24225"));
			Assert.That(line.Period, Is.EqualTo("01.10.2013"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(TrediFarmCheboksarySpecialParser.CheckFileFormat(TrediFarmCheboksarySpecialParser.Load(@"..\..\Data\Waybills\TrediFarmCheboksary.dbf")));
		}
	}
}