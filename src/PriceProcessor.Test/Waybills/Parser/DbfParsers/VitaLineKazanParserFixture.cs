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
	public class VitaLineKazanParserFixture
	{
		[Test]
		public void Parce()
		{
			Assert.IsTrue(VitaLineKazanParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\nakl945976_.dbf")));
			var document = WaybillParser.Parse("nakl945976_.dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("945976"));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("100666"));
			Assert.That(line.Product, Is.EqualTo("АВЕНТ бутылочка д/кормления №2 125мл /86040/ полипропилен"));
			Assert.That(line.Quantity, Is.EqualTo(1.00));

			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0.00));
			Assert.That(line.ProducerCost, Is.EqualTo(342.86));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(380.51));
			Assert.That(line.SupplierCost, Is.EqualTo(449.00));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));

			Assert.That(line.Nds, Is.EqualTo(18));

			Assert.That(line.Amount, Is.EqualTo(449.00));
			Assert.That(line.NdsAmount, Is.EqualTo(68.49));

			Assert.That(line.Producer, Is.EqualTo("ФИЛИПС ЭЛЕКТРОНИКС ЮК"));
			Assert.That(line.Country, Is.EqualTo("ВЕЛИКОБРИТАНИЯ"));

			Assert.That(line.Period, Is.EqualTo("01.12.2013"));
			Assert.That(line.SerialNumber, Is.EqualTo("Б/С"));
			Assert.That(line.Unit, Is.EqualTo("уп."));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130090/310511/0044"));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.EAN13, Is.EqualTo("8710103495857"));

			Assert.That(line.Certificates, Is.EqualTo("GB.АВ36.Д04010"));
			Assert.That(line.CertificatesDate, Is.EqualTo(null));
		}
	}
}
