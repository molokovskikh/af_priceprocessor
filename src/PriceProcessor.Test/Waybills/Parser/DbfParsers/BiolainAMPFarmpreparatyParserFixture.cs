using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;


namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BiolainAMPFarmpreparatyParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\17913.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(3));

			var line = document.Lines[1];

			Assert.That(line.Code, Is.EqualTo("726481"));
			Assert.That(line.Product, Is.EqualTo("Медицинский антисептический раствор~р-р наружн.~70%~фл.100мл N1~Фармацевтический"));
			Assert.That(line.SerialNumber, Is.EqualTo("031211"));
			Assert.That(line.Period, Is.EqualTo("01.12.2016"));
			Assert.That(line.SupplierCost, Is.EqualTo(13.11));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU ФМ01 Д70630"));
			Assert.That(line.CertificatesDate, Is.EqualTo("30.03.2012"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(20.52));
			Assert.That(line.Producer, Is.EqualTo("Фармацевтический комбинат"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11.92));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
		}
		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(BiolainAMPFarmpreparatyParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\17913.dbf")));
		}

	}
}
