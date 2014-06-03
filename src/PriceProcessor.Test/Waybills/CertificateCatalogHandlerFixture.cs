using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Security;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net.Config;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using Test.Support.log4net;
using NHibernate.Linq;
using Common.NHibernate;
using FileHelper = Common.Tools.FileHelper;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class CertificateCatalogHandlerFixture : IntegrationFixture
	{
		private CertificateSource _source;
		private TestSupplier _supplier;
		private IRemoteFtpSource ftpSource;

		[SetUp]
		public void SetUp()
		{
			_supplier = TestSupplier.CreateNaked(session);
			var realSupplier = session.Load<Supplier>(_supplier.Id);

			session.DeleteEach<CertificateSource>();

			_source = new CertificateSource {
				SourceClassName = typeof(RostaCertificateSource).Name,
				SearchInAssortmentPrice = true
			};
			ftpSource = (IRemoteFtpSource)_source.GetCertificateSource();
			_source.Suppliers = new List<Supplier>();
			_source.Suppliers.Add(realSupplier);
			session.Save(_source);
		}

		[Test(Description = "проверяем заполнение таблицы каталога сертификатов")]
		public void ImportCatalogFile()
		{
			FileHelper.InitDir(Path.Combine(Settings.Default.FTPOptBoxPath, "LIST"));
			File.Copy(@"..\..\Data\RostaSertList.dbf", Path.Combine(Settings.Default.FTPOptBoxPath, "LIST", "SERT_LIST.DBF"));
			var rostaCertList = Dbf.Load(@"..\..\Data\RostaSertList.dbf");
			var supplierCode = rostaCertList.Rows[0]["CODE"].ToString();

			//Берем первый попавшийся продукт
			var product = session.Query<TestProduct>().First();
			var price = _supplier.Prices[0];

			//Прайс-лист должен быть ассортиментным
			price.PriceType = PriceType.Assortment;

			price.AddProductSynonym(product.CatalogProduct.Name + " Тестовый", product);
			var synonym = price.ProductSynonyms[price.ProductSynonyms.Count - 1];
			session.Save(price);

			var core = new TestCore(synonym) { Price = price, Code = supplierCode, Quantity = "0", Period = "01.01.2015" };
			session.Save(core);

			var catalogs = session.Query<CertificateSourceCatalog>().Where(c => c.CertificateSource == _source).ToList();

			Assert.That(_source.FtpFileDate, Is.Null, "Дата файла с ftp не должна быть заполнена");
			Assert.That(catalogs.Count, Is.EqualTo(0), "Таблица не должна быть заполнена");

			var catalogFile = new CertificateCatalogFile {
				Source = _source,
				FileDate = DateTime.Now,
				LocalFileName = Path.GetFullPath(@"..\..\Data\RostaSertList.dbf")
			};

			BasicConfigurator.Configure();
			var handler = new CertificateCatalogHandler();
			handler.CreateDownHandlerPath();
			handler.ProcessData();

			session.Refresh(_source);
			Assert.That(_source.FtpFileDate, Is.Not.Null, "Дата файла с ftp должна быть заполнена");
			Assert.That(_source.FtpFileDate.Value.Subtract(catalogFile.FileDate).TotalSeconds, Is.LessThan(1), "Дата файла не совпадает");

			var existsCatalogs = session.Query<CertificateSourceCatalog>().Where(c => c.CertificateSource == _source).ToList();
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
}