using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.CertificateSources;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
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

		[SetUp]
		public void SetUp()
		{
			var supplier = TestSupplier.Create();
			var realSupplier = Supplier.Find(supplier.Id);

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

		[Test(Description = "��������� ���������� ������� �������� ������������")]
		public void ImportCatalogFile()
		{
			Assert.That(_source.FtpFileDate, Is.Null, "���� ����� � ftp �� ������ ���� ���������");
			var catalogs = CertificateSourceCatalog.Queryable.Where(c => c.CertificateSource == _source).ToList();
			Assert.That(catalogs.Count, Is.EqualTo(0), "������� �� ������ ���� ���������");

			using (new SessionScope()) {
				var catalogFile = new CertificateCatalogFile{
					Source = _source,
					FileDate = DateTime.Now,
					LocalFileName = Path.GetFullPath(@"..\..\Data\RostaSertList.dbf")
				};

				var handler = new TestRostaCertificateCatalogHandler();
				handler.TestImportCatalogFile(catalogFile);

				_source.Refresh();
				Assert.That(_source.FtpFileDate, Is.Not.Null, "���� ����� � ftp ������ ���� ���������");
				Assert.That(_source.FtpFileDate.Value.Subtract(catalogFile.FileDate).TotalSeconds, Is.LessThan(1), "���� ����� �� ���������");

				var existsCatalogs = CertificateSourceCatalog.Queryable.Where(c => c.CertificateSource == _source).ToList();
				Assert.That(existsCatalogs.Count, Is.GreaterThan(0), "������� ������ ���� ���������");
			}
		}

		[Test(Description = "��������� ������� ����� � ��������� ������������"), Ignore("��������� ���� � ������ ������")]
		public void GetCatalogFile()
		{
			using (new SessionScope()) {

				var handler = new TestRostaCertificateCatalogHandler();
				handler.TestCreateDownHandlerPath();

				//���������� ������ ������� � ���������� ����
				var catalogFile = handler.TestGetCatalogFile(_source);

				Assert.That(catalogFile, Is.Not.Null);
				Assert.That(catalogFile.Source, Is.EqualTo(_source));
				Assert.That(catalogFile.FileDate, Is.GreaterThan(DateTime.MinValue));
				Assert.IsNotNullOrEmpty(catalogFile.LocalFileName);
				Assert.That(File.Exists(catalogFile.LocalFileName), Is.True);

				File.Delete(catalogFile.LocalFileName);

				//���������� ������ ������� � ���� �� ��������, �.�. �� ��������
				_source.FtpFileDate = catalogFile.FileDate;
				var newCatalogFile = handler.TestGetCatalogFile(_source);

				Assert.That(newCatalogFile, Is.Null, "���� �� ������ ���� �������");
			}
		}

	}
}