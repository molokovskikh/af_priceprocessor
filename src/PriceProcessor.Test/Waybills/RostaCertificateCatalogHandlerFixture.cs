using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	public class TestCertificateCatalogHandler : CertificateCatalogHandler
	{
		public void TestProcessData()
		{
			ProcessData();
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
		private IRemoteFtpSource ftpSource;

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
				ftpSource = (IRemoteFtpSource)_source.GetCertificateSource();
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

				//Прайс-лист должен быть ассортиментным
				price.PriceType = PriceType.Assortment;

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

				var handler = new TestCertificateCatalogHandler();
				handler.ImportCatalogFile(catalogFile, ftpSource);

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

				var handler = new TestCertificateCatalogHandler();
				handler.TestCreateDownHandlerPath();

				//Производим первую закачку и закачиваем файл
				var catalogFile = handler.GetCatalogFile(ftpSource, _source);

				Assert.That(catalogFile, Is.Not.Null);
				Assert.That(catalogFile.Source, Is.EqualTo(_source));
				Assert.That(catalogFile.FileDate, Is.GreaterThan(DateTime.MinValue));
				Assert.IsNotNullOrEmpty(catalogFile.LocalFileName);
				Assert.That(File.Exists(catalogFile.LocalFileName), Is.True);

				File.Delete(catalogFile.LocalFileName);

				//производим вторую закачку и файл не качается, т.к. не обновлен
				_source.FtpFileDate = catalogFile.FileDate;
				var newCatalogFile = handler.GetCatalogFile(ftpSource, _source);

				Assert.That(newCatalogFile, Is.Null, "Файл не должен быть закачен");
			}
		}

		[Test(Description = "проверка загрузки элементов из Core с помощью CreateSQLQuery")]
		public void GetCoreWithQuery()
		{

			//Создаем запись в Core для прайс-листа
			var product = TestProduct.FindFirst();
			TestCore core;
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var price = TestPrice.Find(_supplier.Prices[0].Id);

				//Прайс-лист должен быть ассортиментным
				price.PriceType = PriceType.Assortment;

				price.AddProductSynonym(product.CatalogProduct.Name +  " Тестовый", product);
				var synonym = price.ProductSynonyms[price.ProductSynonyms.Count - 1];
				price.SaveAndFlush();

				core = new TestCore() { Price = price, Code = "123456", ProductSynonym = synonym, Product = product, Quantity = "0", Period = "01.01.2015" };
				core.SaveAndFlush();

				transaction.VoteCommit();
			}

			//Выбираем записи из Core для ассортиментных прайсов поставщиков, которые привязаны к нужному источнику сертификатов
			var cores = SessionHelper.WithSession(
				c => c.CreateSQLQuery(@"
select
	{core.*}
from
	documents.SourceSuppliers ss
	inner join usersettings.PricesData pd on pd.FirmCode = ss.SupplierId
	inner join farm.Core0 {core} on core.PriceCode = pd.PriceCode and pd.PriceType = 1
	inner join catalogs.Products p on p.Id = core.ProductId
where
	ss.CertificateSourceId = :sourceId;
"
					)
					.AddEntity("core", typeof(Core))
					.SetParameter("sourceId", _source.Id)
					.List<Core>());

			Assert.That(cores.Count, Is.GreaterThan(0));
			Assert.That(cores.Count, Is.EqualTo(1));
			Assert.That(cores[0].Code, Is.EqualTo("123456"));
			Assert.That(cores[0].ProductId, Is.Not.Null);
			Assert.That(cores[0].ProductId.HasValue, Is.True);
			Assert.That(cores[0].Product, Is.Not.Null);
			Assert.That(cores[0].Product.Id, Is.EqualTo(cores[0].ProductId.Value));
		}

	}
}