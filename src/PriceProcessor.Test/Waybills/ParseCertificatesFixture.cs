using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class ParseCertificatesFixture
	{
		[Test(Description = "проверка создания заданий для несуществующих сертификатов")]
		public void CheckParse()
		{
			var docSupplier = Supplier.Find(39u);

			var firstCatalog = new Catalog {Id = 1, Name = "catalog1"};
			var secondCatalog = new Catalog {Id = 2, Name = "catalog2"};
			var firstProduct = new Product {Id = 3, CatalogProduct = firstCatalog};
			var secondProduct = new Product {Id = 4, CatalogProduct = firstCatalog};
			var thirdProduct = new Product {Id = 5, CatalogProduct = secondCatalog};

			var document = new Document {
				FirmCode = docSupplier.Id
			};
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = "крутая серия 1"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "крутая серия 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = string.Empty,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(3));
			Assert.That(document.Tasks.TrueForAll(t => t.Supplier.Id == docSupplier.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == firstCatalog.Id && task[0].SerialNumber == "крутая серия 1");
			Assert.That(task[1].CatalogProduct.Id == firstCatalog.Id && task[1].SerialNumber == "крутая серия 2");
			Assert.That(task[2].CatalogProduct.Id == secondCatalog.Id && task[2].SerialNumber == "крутая серия 1");
		}

		[Test(Description = "проверка создания заданий для несуществующих сертификатов при существовании сертификатов")]
		public void CheckParseWithExistsCertificates()
		{
			var docSupplier = Supplier.Find(39u);
			var anotherSupplier = Supplier.Queryable.Where(s => s.Id != docSupplier.Id).First();

			var catalogs = Catalog.Queryable.Take(2).ToList().OrderBy(c => c.Id).ToList();
			var existsCatalog = catalogs[0];
			var nonExistCatalog = catalogs[1];
			var serialNumber = "крутая серия 5";

			var firstProduct = new Product {Id = 3, CatalogProduct = existsCatalog};
			var secondProduct = new Product {Id = 4, CatalogProduct = nonExistCatalog};
			var thirdProduct = new Product {Id = 5, CatalogProduct = existsCatalog};


			using (new TransactionScope()) {
				var certificates =
					Certificate.Queryable.Where(c => c.SerialNumber == serialNumber).ToList();
				certificates.ForEach(c => c.Delete());
			}

			var existsCertificate = new Certificate();
			using (new TransactionScope()) {
				existsCertificate.CatalogProduct = Catalog.Find(existsCatalog.Id);
				existsCertificate.SerialNumber = serialNumber;
				existsCertificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName(),
						Supplier = docSupplier
					}
				);
				existsCertificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName(),
						Supplier = docSupplier
					}
				);
				existsCertificate.NewFile(
					new CertificateFile{
						OriginFilename = Path.GetRandomFileName(),
						Supplier = anotherSupplier
					}
				);
				existsCertificate.Create();
			}

			var document = new Document{
				FirmCode = docSupplier.Id
			};
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = serialNumber
			});
			document.NewLine(new DocumentLine{
				ProductEntity = firstProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = secondProduct,
				SerialNumber = "крутая серия 2",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = serialNumber,
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "крутая серия 1",
				CertificateFilename = "cerFilename"
			});
			document.NewLine(new DocumentLine{
				ProductEntity = thirdProduct,
				SerialNumber = "крутая серия 2",
				CertificateFilename = "cerFilename"
			});

			CertificateSourceDetector.DetectAndParse(document);

			Assert.That(document.Tasks.Count, Is.EqualTo(4));
			Assert.That(document.Tasks.TrueForAll(t => t.Supplier.Id == docSupplier.Id));

			var task = document.Tasks.OrderBy(t => t.CatalogProduct.Id).ThenBy(t => t.SerialNumber).ToList();
			Assert.That(task[0].CatalogProduct.Id == existsCatalog.Id && task[0].SerialNumber == "крутая серия 1");
			Assert.That(task[1].CatalogProduct.Id == existsCatalog.Id && task[1].SerialNumber == "крутая серия 2");
			Assert.That(task[2].CatalogProduct.Id == nonExistCatalog.Id && task[2].SerialNumber == "крутая серия 2");
			Assert.That(task[3].CatalogProduct.Id == nonExistCatalog.Id && task[3].SerialNumber == serialNumber);

			Assert.That(document.Lines[0].Certificate.Id, Is.EqualTo(existsCertificate.Id));
			Assert.That(document.Lines[4].Certificate.Id, Is.EqualTo(existsCertificate.Id));
		}

	}
}