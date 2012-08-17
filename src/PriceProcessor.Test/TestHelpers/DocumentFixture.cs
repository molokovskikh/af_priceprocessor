using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.TestHelpers
{
	public class DocumentFixture
	{
		protected TestClient client;
		protected TestSupplier supplier;
		protected TestPrice price;
		protected TestAddress testAddress;

		protected Supplier appSupplier;
		protected WaybillSettings settings;
		protected Address address;

		protected string docRoot;
		protected string waybillsPath;

		[SetUp]
		public void Setup()
		{
			client = TestClient.Create();
			testAddress = client.Addresses[0];
			using (new SessionScope()) {
				address = Address.Find(testAddress.Id);
				settings = WaybillSettings.Find(client.Id);
				price = TestSupplier.CreateTestSupplierWithPrice();
				supplier = price.Supplier;
				appSupplier = Supplier.Find(supplier.Id);
			}
			docRoot = Path.Combine(Settings.Default.DocumentPath, address.Id.ToString());
			waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
		}

		public TestDocumentLog CreateTestLog(string file)
		{
			var log = new TestDocumentLog(supplier, testAddress, file);
			using (new TransactionScope())
				log.SaveAndFlush();

			File.Copy(@"..\..\Data\Waybills\" + file, Path.Combine(waybillsPath, String.Format("{0}_{1}({2}){3}",
				log.Id,
				supplier.Name,
				Path.GetFileNameWithoutExtension(file),
				Path.GetExtension(file))));
			return log;
		}

		public TestDocumentLog[] CreateTestLogs(params string[] files)
		{
			return files.Select(CreateTestLog).ToArray();
		}
	}
}