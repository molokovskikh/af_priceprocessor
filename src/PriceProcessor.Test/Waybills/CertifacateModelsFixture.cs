using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net.Config;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class CertifacateModelsFixture
	{
		private TestWaybillLine CreateBodyLine()
		{
			var supplier = (TestSupplier)TestSupplier.Queryable.First();
			var user = TestUser.Queryable.First(u => u.AvaliableAddresses.Count > 0);

			var documentLog = new TestDocumentLog {
				FirmCode = supplier.Id,
				ClientCode = user.Client.Id,
				DocumentType = DocumentType.Waybill,
				LogTime = DateTime.Now,
				FileName = Path.GetRandomFileName() + ".txt"
			};

			var document = new TestWaybill {
				ClientCode = user.Client.Id,
				FirmCode = supplier.Supplier,
				DocumentType = DocumentType.Waybill,
				WriteTime = DateTime.Now
			};

			var documentLine = new TestWaybillLine();
			documentLine.Waybill = document;

			document.Lines = new List<TestWaybillLine>();
			document.Lines.Add(documentLine);

			using (new TransactionScope()) {
				documentLog.Create();
				document.DownloadId = documentLog.Id;
				document.Create();
				documentLine.Create();
			}

			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.Lines[0].Id, Is.GreaterThan(0));

			return documentLine;
		}

		[Test(Description = "������� ������ �� ������ �����������")]
		public void SimpleCreateTask()
		{
			var documentLine = CreateBodyLine();
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = Path.GetRandomFileName();
			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var task = new CertificateTask();
			using (new TransactionScope()) {
				task.CatalogProduct = Catalog.Find(catalog.Id);
				task.SerialNumber = serialNumber;
				task.DocumentLine = realDocumentLine;
				task.Create();
			}

			Assert.That(task.Id, Is.GreaterThan(0));
		}

		[Test(Description = "������� ������ �� ������ ����������� � ����������� ����������� �����")]
		public void CreateTaskOnUniqueKey()
		{
			var documentLine = CreateBodyLine();
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = "���� ���� ����";
			var realDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			using (new TransactionScope()) {
				var certificateTasks =
					CertificateTask.Queryable.Where(c => c.SerialNumber.Equals(serialNumber)).ToList();
				certificateTasks.ForEach(c => c.Delete());
			}

			var task = new CertificateTask();
			using (new TransactionScope()) {
				task.CatalogProduct = Catalog.Find(catalog.Id);
				task.SerialNumber = serialNumber;
				task.DocumentLine = realDocumentLine;
				task.Create();
			}

			Assert.That(task.Id, Is.GreaterThan(0));

			var doubleDocumentLine = CreateBodyLine();
			var doubleRealDocumentLine = Document.Find(documentLine.Waybill.Id).Lines[0];

			var doubleTask = new CertificateTask {
				CatalogProduct = task.CatalogProduct,
				SerialNumber = "���� ���� ����",
				DocumentLine = doubleRealDocumentLine
			};

			try {
				using (new TransactionScope()) {
					doubleTask.Create();
				}

				Assert.Fail("��� ���������� ������ ���� �������� ���������� � ���������� ����������� �����");
			}
			catch (Exception exception) {
				if (!ExceptionHelper.IsDuplicateEntryExceptionInChain(exception))
					throw;
			}
		}

		[Test(Description = "������� ����������")]
		public void SimpleCreateCertifacate()
		{
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = Path.GetRandomFileName();

			var certificate = new Certificate();
			using (new TransactionScope()) {
				certificate.CatalogProduct = Catalog.Find(catalog.Id);
				certificate.SerialNumber = serialNumber;
				certificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName()
					}
				);
				certificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName()
					}
				);
				certificate.Create();
			}

			Assert.That(certificate.Id, Is.GreaterThan(0));
			Assert.That(certificate.CertificateFiles.ToList().TrueForAll(f => f.Id > 0));
		}

		[Test(Description = "������� ���������� � ����������� ����������� �����")]
		public void CreateCertifacateOnUniqueKey()
		{
			var catalog = TestCatalogProduct.Queryable.First();
			var serialNumber = "���� ���� ����";

			using (new TransactionScope()) {
				var certifacates =
					Certificate.Queryable.Where(c => c.SerialNumber.Equals(serialNumber)).ToList();
				certifacates.ForEach(c => c.Delete());
			}

			var certificate = new Certificate();
			using (new TransactionScope()) {
				certificate.CatalogProduct = Catalog.Find(catalog.Id);
				certificate.SerialNumber = serialNumber;
				certificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName()
					}
				);
				certificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName()
					}
				);
				certificate.Create();
			}

			Assert.That(certificate.Id, Is.GreaterThan(0));
			Assert.That(certificate.CertificateFiles.ToList().TrueForAll(f => f.Id > 0));

			var double�ertificate = new Certificate {
				CatalogProduct = certificate.CatalogProduct,
				SerialNumber = "���� ���� ����"
			};

			try {
				using (new TransactionScope()) {
					double�ertificate.Create();
				}

				Assert.Fail("��� ���������� ������ ���� �������� ���������� � ���������� ����������� �����");
			}
			catch (Exception exception) {
				if (!ExceptionHelper.IsDuplicateEntryExceptionInChain(exception))
					throw;
			}
		}

	}
}
	