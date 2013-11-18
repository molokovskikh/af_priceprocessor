using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.TestHelpers
{
	public class BaseWaybillHandlerFixture : IntegrationFixture
	{
		protected TestAddress address;
		protected TestClient client;
		protected TestSupplier supplier;

		protected string waybillDir;
		protected string rejectDir;

		protected void SetConvertFormat()
		{
			var settings = client.Settings;
			settings.IsConvertFormat = true;
			settings.AssortmentPriceId = supplier.Prices.First().Id;
			settings.SaveAndFlush();
		}

		protected void CheckClientDirectory(int waitingFilesCount, DocType documentsType, TestAddress address = null)
		{
			var savedFiles = GetFileForAddress(documentsType, address);
			Assert.That(savedFiles.Count(), Is.EqualTo(waitingFilesCount));
		}

		protected IList<TestDocumentLog> CheckDocumentLogEntry(int waitingCountEntries, TestAddress address = null)
		{
			if (address == null)
				address = client.Addresses[0];

			var logs = TestDocumentLog.Queryable.Where(log =>
				log.Client.Id == client.Id &&
					log.Supplier.Id == supplier.Id &&
					log.Address == address);
			Assert.That(logs.Count(), Is.EqualTo(waitingCountEntries));

			return logs.ToList();
		}

		protected void CheckDocumentEntry(int waitingCountEntries)
		{
			var documents = Document.Queryable.Where(doc => doc.FirmCode == supplier.Id &&
				doc.ClientCode == client.Id &&
				doc.Address.Id == client.Addresses[0].Id);
			Assert.That(documents.Count(), Is.EqualTo(waitingCountEntries));
		}

		protected string[] GetFileForAddress(DocType documentsType, TestAddress address = null)
		{
			if (address == null)
				address = client.Addresses[0];
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, address.Id.ToString().PadLeft(3, '0'));
			return Directory.GetFiles(Path.Combine(clientDirectory, documentsType + "s"), "*.*", SearchOption.AllDirectories);
		}

		protected void SetDeliveryCodes(uint? supplierDeliveryId)
		{
			var price = supplier.Prices[0];
			session.Refresh(price);
			var intersection = price.Intersections.First(i => i.Client.Id == client.Id);
			var addressIntersection = intersection.AddressIntersections.First(i => i.Address.Id == address.Id);
			addressIntersection.SupplierDeliveryId = supplierDeliveryId.ToString();
			addressIntersection.Save();
		}

		public string CreateSupplierDir(DocType type)
		{
			var directory = Path.Combine(Settings.Default.FTPOptBoxPath, supplier.Id.ToString());
			directory = Path.Combine(directory, type + "s");

			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
			Directory.CreateDirectory(directory);
			return directory;
		}

		protected void MaitainAddressIntersection(uint addressId, string supplierDeliveryId = null, string supplierClientId = null)
		{
			if (String.IsNullOrEmpty(supplierDeliveryId))
				supplierDeliveryId = addressId.ToString();

			var command = new MySqlCommand(@"
insert into Customers.AddressIntersection(AddressId, IntersectionId, SupplierDeliveryId)
select a.Id, i.Id, ?supplierDeliveryId
from Customers.Intersection i
join Customers.Addresses a on a.ClientId = i.ClientId
left join Customers.AddressIntersection ai on ai.AddressId = a.Id and ai.IntersectionId = i.Id
where
a.Id = ?AddressId
and ai.Id is null", (MySqlConnection)session.Connection);

			command.Parameters.AddWithValue("?AddressId", addressId);
			command.Parameters.AddWithValue("?supplierDeliveryId", supplierDeliveryId);
			var insertCount = command.ExecuteNonQuery();
			if (insertCount == 0) {
				command.CommandText = @"
update
Customers.Intersection i,
Customers.Addresses a,
Customers.AddressIntersection ai
set
ai.SupplierDeliveryId = ?supplierDeliveryId,
i.SupplierClientId = ?supplierClientId
where
a.ClientId = i.ClientId
and ai.AddressId = a.Id
and ai.IntersectionId = i.Id
and a.Id = ?AddressId
";
				command.Parameters.AddWithValue("?supplierClientId", supplierClientId);
				command.ExecuteNonQuery();
			}
		}

		protected void SetConvertDocumentSettings()
		{
			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == client.Id).SingleOrDefault();
			//запоминаем начальное состояние настройки
			var isConvertFormat = settings.IsConvertFormat;
			//и если оно не включено, то включим принудительно для теста
			if (!isConvertFormat) {
				settings.IsConvertFormat = true;
				settings.AssortmentPriceId = supplier.Prices.First().Id;
				settings.SaveAndFlush();
			}
		}

		protected void CopyFilesFromDataDirectory(string[] fileNames)
		{
			var dataDirectory = Path.GetFullPath(Settings.Default.TestDataDirectory);
			// Копируем файлы в папку поставщика
			foreach (var fileName in fileNames)
				File.Copy(Path.Combine(dataDirectory, fileName), Path.Combine(waybillDir, fileName));
		}
	}
}