using NUnit.Framework;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Common.Tools;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class ASM5836ParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(ASM5836Parser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\1005786.DBF")));
			var document = WaybillParser.Parse("1005786.DBF");

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("16089"));
			Assert.That(line.Product, Is.EqualTo("прокладка д/рожениц саму ворлаген уп №10"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCost, Is.EqualTo(206.13));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(37.48));
			Assert.That(line.Amount, Is.EqualTo(412.26));
			Assert.That(line.SerialNumber, Is.EqualTo("7164130"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ИМ34.Д00204"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130142/190612/0002"));
			Assert.That(line.Country, Is.EqualTo("германия"));
			Assert.That(line.Producer, Is.EqualTo("пауль хартманн"));
		}
	}
}