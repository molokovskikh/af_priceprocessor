using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor;
using NHibernate.Linq;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Handlers
{
	[TestFixture]
	public class WaybillFtpSourceHandlerIntegrationFixture : IntegrationFixture
	{
		private string ftpHost;
		private int ftpPort;
		private string ftpWaybillDirectory;
		private string user;
		private string password;
		private uint supplierDeliveryId;
		private string ftpRejectDirectory;
		private TestAddress address;
		private TestClient client;
		private TestSupplier supplier;

		private WaybillFtpSourceHandlerForTesting handler;

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();

			var supplierFtpSources = session.Query<TestWaybillSource>().Where(source =>
				source.SourceType == WaybillSourceType.FtpSupplier);
			foreach (var source in supplierFtpSources) {
				source.SourceType = WaybillSourceType.ForTesting;
				Save(source);
			}

			handler = new WaybillFtpSourceHandlerForTesting();

			ftpHost = "ftp.narod.ru";
			ftpPort = 21;
			ftpWaybillDirectory = "Waybills";
			ftpRejectDirectory = "Rejects";
			user = "test";
			password = "test";
			supplierDeliveryId = 1234u;

			client = TestClient.Create();
			client.Save();

			address = client.Addresses[0];

			supplier = CreateAndSetupSupplier(ftpHost, ftpPort, ftpWaybillDirectory, ftpRejectDirectory, user, password);

			CopyWaybillFiles();
		}

		[TearDown]
		public void Stop()
		{
			var supplierFtpSources = session.Query<TestWaybillSource>().Where(source =>
				source.SourceType == WaybillSourceType.ForTesting);
			foreach (var source in supplierFtpSources) {
				source.SourceType = WaybillSourceType.FtpSupplier;
				Save(source);
			}
		}

		[Test]
		public void SetPassiveModeTest()
		{
			session.Clear();
			supplier.WaybillSource.FtpActiveMode = true;
			Save(supplier.WaybillSource);
			session.Flush();
			Reopen();
			handler.Process();
			Assert.That(handler.TestFtpPassiveMode, Is.False);
		}

		[Test]
		public void SetPassiveModeDefaultTest()
		{
			handler.Process();
			Assert.That(handler.TestFtpPassiveMode, Is.True);
		}

		private TestSupplier CreateAndSetupSupplier(string ftpHost, int ftpPort, string ftpWaybillDirectory, string ftpRejectDirectory, string user, string password)
		{
			var supplier = TestSupplier.Create();
			var source = supplier.WaybillSource;
			source.SourceType = WaybillSourceType.FtpSupplier;
			source.UserName = user;
			source.Password = password;
			var waybillUri = new UriBuilder("ftp", ftpHost, ftpPort, ftpWaybillDirectory);
			var rejectUri = new UriBuilder("ftp", ftpHost, ftpPort, ftpRejectDirectory);
			source.WaybillUrl = waybillUri.Uri.AbsoluteUri;
			source.RejectUrl = rejectUri.Uri.AbsoluteUri;
			Save(source);
			Save(supplier);
			session.Flush();
			Reopen();
			return supplier;
		}

		private void CopyWaybillFiles()
		{
			SetDeliveryCodes(supplierDeliveryId);
			var waybillsDir = Path.Combine(Settings.Default.FTPOptBoxPath, ftpWaybillDirectory);
			if (!Directory.Exists(waybillsDir))
				Directory.CreateDirectory(waybillsDir);

			var source = @"..\..\Data\Waybills\70983_906384.ZIP";
			var destination = Path.Combine(waybillsDir, String.Format("{0}_{1}", supplierDeliveryId, Path.GetFileName(source)));
			File.Copy(source, destination);
		}

		private void SetDeliveryCodes(uint? supplierDeliveryId)
		{
			var id = supplier.Prices[0].Id;
			var price = session.Load<TestPrice>(id);
			var intersection = price.Intersections.First(i => i.Client.Id == client.Id);
			var addressIntersection = intersection.AddressIntersections.First(i => i.Address.Id == address.Id);
			addressIntersection.SupplierDeliveryId = supplierDeliveryId.ToString();
			Save(addressIntersection);
		}
	}
}
