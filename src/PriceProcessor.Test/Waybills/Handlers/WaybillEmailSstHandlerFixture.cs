using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.Mime;
using NUnit.Framework;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.PriceProcessor.Downloader;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using log4net.Config;
using MimeKit;
using NHibernate;
using NHibernate.Linq;
using MailboxAddress = LumiSoft.Net.Mime.MailboxAddress;

namespace PriceProcessor.Test.Waybills.Handlers
{
	public class WaybillEmailSstHandlerForTesting : WaybillEmailProtekHandler
	{
		/// <summary>
		/// директория для сохранения файлов для истории
		/// </summary>
		protected string DownHistoryPath;
		public List<Mime> Sended = new List<Mime>();
		public List<MimeMessage> SendedMessages = new List<MimeMessage>();

		public void CreateDirectoryPath()
		{
			CreateDownHandlerPath();
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder);

			DownHistoryPath = Settings.Default.HistoryPath;
			if (!Directory.Exists(DownHistoryPath))
				Directory.CreateDirectory(DownHistoryPath);
		}

		protected override void SendMessage(MimeMessage responseMime)
		{
			SendedMessages.Add(responseMime);
		}
	}

	[TestFixture]
	public class WaybillEmailSstHandlerFixture : IntegrationFixture2
	{
		private bool IsEmlFile;
		private WaybillEmailSstHandlerForTesting handler;
		private EventFilter<WaybillService> filter;
		private TestAddressIntersection aIntersection;
		private int recipientId;
		private Dictionary<TestSupplier, int> supplierList;
		private TestClient client;
		private TestSupplier supplier;

		[SetUp]
		public void DeleteDirectories()
		{
			TestHelper.RecreateDirectories();
			filter = new EventFilter<WaybillService>();
			handler = new WaybillEmailSstHandlerForTesting();
			handler.CreateDirectoryPath();
		}

		[TearDown]
		public void TearDown()
		{
			filter.Reset();
			var events = filter.Events
				.Where(e => e.ExceptionObject.Message != "Не удалось определить тип парсера")
				.ToArray();
			Assert.That(events, Is.Empty, filter.Events.Implode(e => e.ExceptionObject));
			if (session.Transaction.IsActive) {
				session.Flush();
				session.Transaction.Commit();
			}
		}

		private Document GetDocument(int recipientId)
		{
			var document = new Document();
			document.SetInvoice();
			document.Invoice.RecipientId = recipientId;
			document.DocumentDate = SystemTime.Now();
			document.Lines = new List<DocumentLine>();
			for (int i = 0; i < 10; i++) {
				document.Lines.Add(new DocumentLine() {
					Code = "",
					Producer = "", //2_ Название производителя препарата;
					Country = "", //3_ Название страны производителя;
					Quantity = 2, //4_ Количество;
					ProducerCostWithoutNDS = i, //6_ Цена производителя без НДС;
					SupplierCostWithoutNDS = i, //7_ Цена поставщика без НДС (цена Протека без НДС);
					SupplierCost = i, //8_ Цена поставщика с НДС (Резерв);
					SupplierPriceMarkup = i, //9_ Наценка посредника (Торговая надбавка оптового звена);
					ExpireInMonths = i, //1i_ Заводской срок годности в месяцах;
					BillOfEntryNumber = "", //11_ Грузовая Таможенная Декларация (ГТД);
					Certificates = "", //12 Серии сертификатов
					SerialNumber = "", //13 Здесь должно быть : Серия производителя
					DateOfManufacture = SystemTime.Now(), //14_ Здесь должно быть : Дата выпуска препарата;
					Period = "", //15_ Здесь должно быть : Дата истекания срока годности данной серии;
					EAN13 = "", //16_ Штрих-код производителя;
					RegistryDate = SystemTime.Now(), //17_ Здесь должно быть : Дата регистрации цены  в реестре;
					RegistryCost = 1 //18_  Реестровая цена  в рублях;
				});
			}
			return document;
		}

		private MemoryStream GetMemoryStreamForDocument(Document document)
		{
			var fsData = new MemoryStream();
			var fs = new MemoryStream();
			using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1251))) {
				SstExport.SaveLong(document, sw);
				sw.Flush();
				fs.Seek(0, SeekOrigin.Begin);
				fs.CopyTo(fsData);
			}
			return fsData;
		}

		private void AddAttachmentToMessage(MimeMessage mime, MemoryStream msData, string fileName = "text.sst")
		{
			var attachment = new MimePart("text", "plain") {
				ContentObject = new ContentObject(msData),
				ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
				ContentTransferEncoding = ContentEncoding.Base64,
				FileName = fileName
			};

			var multipart = new Multipart("mixed");
			multipart.Add(attachment);
			foreach (var item in mime.BodyParts) {
				multipart.Add(item);
			}
			mime.Body = multipart;
		}

		private void SetSupplierAndClientConnection(int numberOfSuppliers = 1, bool sameRegion = true)
		{
			supplierList = new Dictionary<TestSupplier, int>();
			//новый клиент
			client = TestClient.CreateNaked(session, 2, 2);
			recipientId = new Random().Next(100000, 999999);
			for (int i = 0; i < numberOfSuppliers; i++) {
				//новый поставщик
				supplier = TestSupplier.CreateNaked(session);
				//настройка для отправки почты
				var source = supplier.Prices[0].Costs[0].PriceItem.Source;
				source.SourceType = PriceSourceType.Email;
				source.EmailTo = $"to_{supplier.Id}@sup.com";
				source.EmailFrom = $"from_{supplier.Id}@sup.com";

				//связь с клиентом ->>
				if (sameRegion) {
					supplier.RegionMask = client.MaskRegion;
				}
				session.Save(supplier);
				while (recipientId != new Random().Next(100000, 999999)) {
					recipientId = new Random().Next(100000, 999999);
				}
				var prices = supplier.Prices.FirstOrDefault();
				var intersection = new TestIntersection(prices, client);
				aIntersection = new TestAddressIntersection(client.Addresses.FirstOrDefault(), intersection);
				intersection.Price = prices;
				aIntersection.SupplierDeliveryId = recipientId.ToString();
				session.Save(intersection);
				session.Save(aIntersection);
				session.Flush();
				//<<- связь с клиентом |

				supplierList.Add(supplier, recipientId);
			}
		}

		public static Mime BuildMessageWithAttachments(string to, string from, string fileName, byte[] file)
		{
			var fromAddresses = new AddressList();
			fromAddresses.Parse(@from);
			var responseMime = new Mime();
			responseMime.MainEntity.From = fromAddresses;
			var toList = new AddressList {new MailboxAddress(to)};
			responseMime.MainEntity.To = toList;
			responseMime.MainEntity.Subject = "[Debug message]";
			responseMime.MainEntity.ContentType = MediaType_enum.Multipart_mixed;

			var testEntity = responseMime.MainEntity.ChildEntities.Add();
			testEntity.ContentType = MediaType_enum.Text_plain;
			testEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
			testEntity.DataText = "";

			var attachEntity = responseMime.MainEntity.ChildEntities.Add();
			attachEntity.ContentType = MediaType_enum.Application_octet_stream;
			attachEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
			attachEntity.ContentDisposition = ContentDisposition_enum.Attachment;
			attachEntity.ContentDisposition_FileName = Path.GetFileName(fileName);
			attachEntity.Data = file;

			return responseMime;
		}

		[Test]
		public void MessageClientCheck()
		{
			SetSupplierAndClientConnection();
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			//новая накладная
			var document = GetDocument(recipientId);
			//прикрепление накладной к письму
			var msData = GetMemoryStreamForDocument(document);

			var message = BuildMessageWithAttachments($"to_{supplier.Id}@sup.com",
				$"from_{supplier.Id}@sup.com", "text.sst", msData.ToArray());

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, message.ToByteData());

			//запуск обработчика
			session.Transaction.Commit();

#if DEBUG
			handler.SetSessionForTest(session);
#endif
			handler.ProcessData();
			session.Flush();
			//проверка на наличие документов после запуска обработчика
			currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			//документ был один
			var currentDocument = currentDocuments.FirstOrDefault();
			Assert.IsTrue(currentDocuments.Count() == 1);
			//проверки соответствия полей
			Assert.IsTrue(currentDocument.Address.Id == aIntersection.Address.Id);
			Assert.IsTrue(currentDocument.ClientCode == client.Id);
			Assert.IsTrue(currentDocument.FirmCode == supplier.Id);
		}

		[Test]
		public void SendAMessage()
		{
			SetSupplierAndClientConnection();
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			//подготовка обработчика
			handler.CreateDirectoryPath();

			//новое сообщение
			var mime = TestMessage();

			//новая накладная
			var document = GetDocument(recipientId);
			//прикрепление накладной к письму
			var msData = GetMemoryStreamForDocument(document);
			AddAttachmentToMessage(mime, msData);

			//запуск обработчика
			session.Transaction.Commit();
			handler.ProcessMessage(session, mime);
			//проверка на наличие документов после запуска обработчика
			currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			//документ был один
			var currentDocument = currentDocuments.FirstOrDefault();
			Assert.IsTrue(currentDocuments.Count() == 1);
			//проверки соответствия полей
			Assert.IsTrue(currentDocument.Address.Id == aIntersection.Address.Id);
			Assert.IsTrue(currentDocument.ClientCode == client.Id);
			Assert.IsTrue(currentDocument.FirmCode == supplier.Id);
		}

		private MimeMessage TestMessage()
		{
			var mime = new MimeMessage {Subject = "Тестовое сообщение"};
			mime.To.Add(new MimeKit.MailboxAddress("SomeAddress", $"to_{supplier.Id}@sup.com"));
			mime.From.Add(new MimeKit.MailboxAddress(supplier.Id.ToString(), $"from_{supplier.Id}@sup.com"));
			return mime;
		}

		[Test]
		public void SendAMessageInArchive()
		{
			SetSupplierAndClientConnection();
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			var mime = TestMessage();

			//новая накладная
			var document = GetDocument(recipientId);
			//прикрепление накладной к письму
			var msData = GetMemoryStreamForDocument(document);

			var fsMemoryStream = new MemoryStream();
			using (var s = new ZipOutputStream(fsMemoryStream)) {
				s.SetLevel(5);
				var buffer = msData.ToArray();
				var entry = new ZipEntry("text.sst");
				s.PutNextEntry(entry);
				s.Write(buffer, 0, buffer.Length);
				s.IsStreamOwner = false;
				s.Finish();
				s.Close();
				fsMemoryStream.Position = 0;
			}
			AddAttachmentToMessage(mime, fsMemoryStream, "text.zip");

			//запуск обработчика
			session.Transaction.Commit();
			handler.ProcessMessage(session, mime);

			//проверка на наличие документов после запуска обработчика
			currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			//документ был один
			var currentDocument = currentDocuments.FirstOrDefault();
			Assert.IsTrue(currentDocuments.Count() == 1);
			//проверки соответствия полей
			Assert.IsTrue(currentDocument.Address.Id == aIntersection.Address.Id);
			Assert.IsTrue(currentDocument.ClientCode == client.Id);
			Assert.IsTrue(currentDocument.FirmCode == supplier.Id);
		}


		[Test]
		public void SendAMessageWithoutContent()
		{
			SetSupplierAndClientConnection();
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			var mime = TestMessage();

			//запуск обработчика
			session.Transaction.Commit();
			try {
				handler.ProcessMessage(session, mime);
			} catch (Exception e) {
				Assert.IsTrue(handler.SendedMessages.Count == 2);
				Assert.IsTrue(handler.SendedMessages[0].TextBody.IndexOf("Отсутствуют вложения в письме от адреса") != -1);
				Assert.IsTrue(handler.SendedMessages[1].TextBody.IndexOf("Письмо не распознано.") != -1);
				Assert.IsTrue(e.Message.IndexOf("Письмо не распознано.",StringComparison.Ordinal) != -1);
			}
		}

		[Test]
		public void SendAMessageWithWrongRegionSupplier()
		{
			SetSupplierAndClientConnection(sameRegion: false);
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			var mime = TestMessage();
			//новая накладная
			var document = GetDocument(recipientId);
			//прикрепление накладной к письму
			var msData = GetMemoryStreamForDocument(document);
			AddAttachmentToMessage(mime, msData);

			//запуск обработчика
			session.Transaction.Commit();
			handler.ProcessMessage(session, mime);

			Assert.IsTrue(handler.SendedMessages.Count == 1);
			var messageError = handler.SendedMessages.First();
			Assert.IsTrue(
					messageError.TextBody.IndexOf("Адрес доставки") != -1 &&
					messageError.TextBody.IndexOf("не доступен поставщику") != -1);

			//проверка на наличие документов после запуска обработчика
			currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(currentDocuments.Count() == 0);
		}

		[Test]
		public void SendAMessageWithBrokenFile()
		{
			SetSupplierAndClientConnection();
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			var mime = TestMessage();

			//новая накладная
			var msData = new MemoryStream();
			AddAttachmentToMessage(mime, msData);

			//запуск обработчика
			session.Transaction.Commit();
			try {
				handler.ProcessMessage(session, mime);
			} catch (Exception e) {
				Assert.IsTrue(
					e.Message.IndexOf("при определении формата файла в методе CheckFileFormat возникла ошибка",
						StringComparison.Ordinal) != -1);
			}
		}

		[Test]
		public void SendAMessageWithManySuppliers()
		{
			SetSupplierAndClientConnection(numberOfSuppliers: 3);
			var currentDocuments = new List<Document>();
			foreach (var item in supplierList) {
				uint? value = (uint?) item.Value;
				//проверка на отсутствие документов до запуска обработчика
				var currentDocumentsItem = session.Query<Document>().FirstOrDefault(s => s.Invoice.RecipientId == value);
				if (currentDocumentsItem != null) {
					currentDocuments.Add(currentDocumentsItem);
				}
			}
			Assert.IsTrue(!currentDocuments.Any());


			//новое сообщение
			var mime = TestMessage();
			foreach (var item in supplierList) {
				mime.To.Add(new MimeKit.MailboxAddress(item.Key.Id.ToString(), $"to_{item.Key.Id}@sup.com"));

				//новая накладная
				var document = GetDocument(item.Value);
				//прикрепление накладной к письму
				var msData = GetMemoryStreamForDocument(document);
				AddAttachmentToMessage(mime, msData);
			}

			//запуск обработчика
			session.Transaction.Commit();
			handler.ProcessMessage(session, mime);

			//проверка на наличие документов после запуска обработчикаcurrentDocuments = new List<Document>();
			foreach (var item in supplierList) {
				uint? value = (uint?) item.Value;
				//проверка на отсутствие документов до запуска обработчика
				var currentDocumentsItem = session.Query<Document>().FirstOrDefault(s => s.Invoice.RecipientId == value);
				if (currentDocumentsItem != null) {
					currentDocuments.Add(currentDocumentsItem);
					var aintersection =
						session.Query<TestAddressIntersection>().FirstOrDefault(s => s.SupplierDeliveryId == value.ToString());
					//проверки соответствия полей
					Assert.IsTrue(currentDocumentsItem.Address.Id == aintersection.Address.Id);
					Assert.IsTrue(currentDocumentsItem.ClientCode == client.Id);
					Assert.IsTrue(currentDocumentsItem.FirmCode == item.Key.Id);
				}
			}
			Assert.IsTrue(currentDocuments.Count() == 3);
		}

		[Test]
		public void SendAMessageFileFromTask()
		{
			SetSupplierAndClientConnection();
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			//новое сообщение
			var mime = TestMessage();

			//накладная из задачи
			var document = File.ReadAllText(@"..\..\Data\Waybills\WaybillEmailSstHandlerFixture.sst", Encoding.GetEncoding(1251));
			document = string.Format(document, recipientId);
			byte[] byteArray = Encoding.GetEncoding(1251).GetBytes(document);
			MemoryStream msData = new MemoryStream(byteArray);
			AddAttachmentToMessage(mime, msData);

			//запуск обработчика
			session.Transaction.Commit();
			handler.ProcessMessage(session, mime);

			//проверка на наличие документов после запуска обработчика
			currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			//документ был один
			var currentDocument = currentDocuments.FirstOrDefault();
			Assert.IsTrue(currentDocuments.Count() == 1);
			//проверки соответствия полей
			Assert.IsTrue(currentDocument.Address.Id == aIntersection.Address.Id);
			Assert.IsTrue(currentDocument.ClientCode == client.Id);
			Assert.IsTrue(currentDocument.FirmCode == supplier.Id);
		}


		[Test]
		public void SendAMessageWithoutSupplier()
		{
			SetSupplierAndClientConnection();
			//проверка на отсутствие документов до запуска обработчика
			var currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(!currentDocuments.Any());

			var mime = new MimeMessage {Subject = "Тестовое сообщение"};
			mime.To.Add(new MimeKit.MailboxAddress("SomeAddress", "to_nope@sup.com"));
			mime.From.Add(new MimeKit.MailboxAddress(supplier.Id.ToString(), "from_nope@sup.com"));


			//новая накладная
			var document = GetDocument(recipientId);
			//прикрепление накладной к письму
			var msData = GetMemoryStreamForDocument(document);
			AddAttachmentToMessage(mime, msData);

			//запуск обработчика
			session.Transaction.Commit();

			handler.ProcessMessage(session, mime);

			Assert.IsTrue(handler.SendedMessages.Count == 1);
			Assert.That(handler.SendedMessages.First().TextBody,
				Does.Contain("Не найдено записи в источниках, соответствующей адресу to_nope@sup.com"));

			//проверка на наличие документов после запуска обработчика
			currentDocuments = session.Query<Document>().Where(s => s.Invoice.RecipientId == recipientId);
			Assert.IsTrue(currentDocuments.Count() == 0);
		}
	}
}