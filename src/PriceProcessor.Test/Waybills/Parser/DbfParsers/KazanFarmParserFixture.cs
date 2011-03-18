using System;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KazanFarmParserFixture
	{
		[Test, Description("Тест для накладной в dbf формате от Казань-Фарм. Открываем специальным образом.")]
		public void Parse()
		{
			var documentLog = CreateDocumentLog(2747u, @"..\..\Data\Waybills\00841174.dbf");
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\00841174.dbf", documentLog);

			Assert.That(document.Lines.Count, Is.EqualTo(8));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Kz0000841174"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("16.03.2011")));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("225251461"));
			Assert.That(line.Product, Is.EqualTo("Анаферон детский таб д/рассасывания №20"));
			Assert.That(line.Producer, Is.EqualTo("Материа Медика"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.ProducerCost, Is.EqualTo(100));
			Assert.That(line.RegistryCost, Is.EqualTo(103.67));
			Assert.That(line.SupplierCost, Is.EqualTo(109.21));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(99.28));
			Assert.That(line.Period, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.Nds.Value, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д48448"));
			Assert.That(line.SerialNumber, Is.EqualTo("8741110"));
			Assert.That(line.SupplierPriceMarkup, Is.Null);

			Assert.That(document.Lines[1].VitallyImportant, Is.False);
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(KazanFarmDbfParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\00841174.dbf")));
		}

		private DocumentReceiveLog CreateDocumentLog(uint supplierId, string fileName)
		{
			DocumentReceiveLog documentLog = null;
			using (new SessionScope())
			{
				var supplier = Supplier.Find(supplierId);
				documentLog = new DocumentReceiveLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(fileName, documentLog) is KazanFarmDbfParser);
			return documentLog;
		}
	}
}
