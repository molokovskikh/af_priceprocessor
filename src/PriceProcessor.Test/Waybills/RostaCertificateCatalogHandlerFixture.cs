﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	public class TestRostaCertificateCatalogHandler : RostaCertificateCatalogHandler
	{

		public void TestProcessData()
		{
			ProcessData();
		}

		public void TestImportCatalogFile(CertificateCatalogFile catalogFile)
		{
			ImportCatalogFile(catalogFile);
		}

		public CertificateCatalogFile TestGetCatalogFile(CertificateSource source)
		{
			return GetCatalogFile(source);
		}

		public void TestCreateDownHandlerPath()
		{
			CreateDownHandlerPath();
			Cleanup();
		}
	}

	[TestFixture]
	public class RostaCertificateCatalogHandlerFixture
	{
		private CertificateSource _source;
		private TestSupplier _supplier;

		[SetUp]
		public void SetUp()
		{
			_supplier = TestSupplier.Create();
			var realSupplier = Supplier.Find(_supplier.Id);

			using (new TransactionScope()) {
				var certificateSources = CertificateSource.Queryable.Where(s => s.SourceClassName == typeof(RostaCertificateSource).Name).ToList();
				certificateSources.ForEach(c => c.Delete());

				_source = new CertificateSource{
					SourceClassName = typeof(RostaCertificateSource).Name
				};
				_source.Suppliers = new List<Supplier>();
				_source.Suppliers.Add(realSupplier);
				_source.Create();
			}
		}

		[Test(Description = "проверяем заполнение таблицы каталога сертификатов")]
		public void ImportCatalogFile()
		{
			var rostaCertList = Dbf.Load(@"..\..\Data\RostaSertList.dbf");
			var supplierCode = rostaCertList.Rows[0]["CODE"].ToString();

			//Берем первый попавшийся продукт
			var product = TestProduct.FindFirst();
			TestCore core;
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var price = TestPrice.Find(_supplier.Prices[0].Id);
				price.AddProductSynonym(product.CatalogProduct.Name +  " Тестовый", product);
				var synonym = price.ProductSynonyms[price.ProductSynonyms.Count - 1];
				price.SaveAndFlush();

				core = new TestCore() { Price = price, Code = supplierCode, ProductSynonym = synonym, Product = product, Quantity = "0", Period = "01.01.2015" };
				core.SaveAndFlush();

				transaction.VoteCommit();
			}

			Assert.That(_source.FtpFileDate, Is.Null, "Дата файла с ftp не должна быть заполнена");
			var catalogs = CertificateSourceCatalog.Queryable.Where(c => c.CertificateSource == _source).ToList();
			Assert.That(catalogs.Count, Is.EqualTo(0), "Таблица не должна быть заполнена");

			using (new SessionScope()) {
				var catalogFile = new CertificateCatalogFile{
					Source = _source,
					FileDate = DateTime.Now,
					LocalFileName = Path.GetFullPath(@"..\..\Data\RostaSertList.dbf")
				};

				var handler = new TestRostaCertificateCatalogHandler();
				handler.TestImportCatalogFile(catalogFile);

				_source.Refresh();
				Assert.That(_source.FtpFileDate, Is.Not.Null, "Дата файла с ftp должна быть заполнена");
				Assert.That(_source.FtpFileDate.Value.Subtract(catalogFile.FileDate).TotalSeconds, Is.LessThan(1), "Дата файла не совпадает");

				var existsCatalogs = CertificateSourceCatalog.Queryable.Where(c => c.CertificateSource == _source).ToList();
				Assert.That(existsCatalogs.Count, Is.GreaterThan(0), "Таблица должна быть заполнена");

				var catalogWithCatalog = existsCatalogs.Where(c => c.SupplierCode == supplierCode).FirstOrDefault();
				Assert.That(catalogWithCatalog, Is.Not.Null, "Позиция не существует");
				Assert.That(catalogWithCatalog.CatalogProduct, Is.Not.Null, "Позиция не сопоставлена с каталогом");
				Assert.That(catalogWithCatalog.CatalogProduct.Id, Is.EqualTo(product.CatalogProduct.Id), "Позиция не сопоставлена с каталогом по значению");

				var catalogWithoutCatalog = existsCatalogs.Where(c => c.SupplierCode != supplierCode).FirstOrDefault();
				Assert.That(catalogWithoutCatalog, Is.Not.Null, "Позиция не существует");
				Assert.That(catalogWithoutCatalog.CatalogProduct, Is.Null, "Позиция не должна быть сопоставлена с каталогом");
			}
		}

		[Test(Description = "проверяем закачку файла с каталогов сертификатов"), Ignore("Проверять надо в ручном режиме")]
		public void GetCatalogFile()
		{
			using (new SessionScope()) {

				var handler = new TestRostaCertificateCatalogHandler();
				handler.TestCreateDownHandlerPath();

				//Производим первую закачку и закачиваем файл
				var catalogFile = handler.TestGetCatalogFile(_source);

				Assert.That(catalogFile, Is.Not.Null);
				Assert.That(catalogFile.Source, Is.EqualTo(_source));
				Assert.That(catalogFile.FileDate, Is.GreaterThan(DateTime.MinValue));
				Assert.IsNotNullOrEmpty(catalogFile.LocalFileName);
				Assert.That(File.Exists(catalogFile.LocalFileName), Is.True);

				File.Delete(catalogFile.LocalFileName);

				//производим вторую закачку и файл не качается, т.к. не обновлен
				_source.FtpFileDate = catalogFile.FileDate;
				var newCatalogFile = handler.TestGetCatalogFile(_source);

				Assert.That(newCatalogFile, Is.Null, "Файл не должен быть закачен");
			}
		}

	}
}