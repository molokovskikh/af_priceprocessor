using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.MySql;
using Common.Tools;
using Dapper;
using Inforoom.Common;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using MimeKit;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;

namespace Inforoom.PriceProcessor.Downloader.MailHandler
{
	public partial class MailKitClient
	{
		public bool ProcessAttachments(ISession session, MimeMessage message, uint supplierId, string emailAuthor)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			var matched = false;

			var attachments = GetValidAttachements(message);
			if (!attachments.Any()) {
				_logger.Info(String.Format($"{nameof(MailKitClient)}: Отсутствуют вложения в письме от адреса {0}", emailAuthor));
				SendPublicErrorMessage(String.Format($"Отсутствуют вложения в письме от адреса {0}", emailAuthor), message);
			}

			using (var cleaner = new FileCleaner()) {
				foreach (var mimeEntity in attachments) {
					var savedFiles = new List<string>();
					//получение текущей директории
					var currentFileName = Path.Combine(DownHandlerPath, GetFileName(mimeEntity));

					//сохранение содержимого в текущую директорию
					using (var fs = new FileStream(currentFileName, FileMode.Create))
						((MimePart) mimeEntity).ContentObject.DecodeTo(fs);

					// нужно учесть, что файл может быть архивом
					var correctArchive = FileHelper.ProcessArchiveIfNeeded(currentFileName, ExtrDirSuffix);

					//если архив не распакован
					if (!correctArchive) {
						DocumentReceiveLog.Log(supplierId, null, Path.GetFileName(currentFileName), DocType.Waybill,
							"Не удалось распаковать файл", Convert.ToInt32(CurrentMesdsageId.Id));
						Cleanup();
						continue;
					}
					//если содержимое является архивом и он был распакован нужно извлеченные файлы добавить в список обрабатываемых (savedFiles) и отправить в обработчик мусора (в cleaner)
					if (ArchiveHelper.IsArchive(currentFileName)) {
						var files = Directory.GetFiles(currentFileName + ExtrDirSuffix +
							Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
						savedFiles.AddRange(files);
						cleaner.Watch(files);
					} else {
						//только с расширения .sst
						if (new FileInfo(currentFileName).Extension.ToLower() == ".sst") {
							savedFiles.Add(currentFileName);
						}
						cleaner.Watch(currentFileName);
					}
					var logs = ProcessWaybillFile(session, savedFiles, supplierId, message);
					//если логи есть, значит файл распознан
					if (logs.Count > 0) {
						matched = true;
						var service = new WaybillService();
						service.Process(logs);
						if (service.Exceptions.Count > 0) {
							SendPublicErrorMessage(service.Exceptions.First().Message, message);
						}
					}
				}
			}
			//удаление временных файлов
			Cleanup();
			return matched;
		}

		public static IEnumerable<MimeEntity> GetValidAttachements(MimeMessage mime)
		{
			return mime.Attachments.Where(m => !String.IsNullOrEmpty(GetFileName(m)) && m.IsAttachment);
		}

		public static string GetFileName(MimeEntity entity)
		{
			if (!String.IsNullOrEmpty(entity.ContentDisposition.FileName))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentDisposition.FileName));
			if (!String.IsNullOrEmpty(entity.ContentType.Name))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentType.Name));
			return null;
		}

		private List<DocumentReceiveLog> ProcessWaybillFile(ISession session, IList<string> files, uint supplierId, MimeMessage mimeMessage)
		{
			var logs = new List<DocumentReceiveLog>();
			foreach (var archiveFile in files) {
				var extractedFiles = new[] {archiveFile};
				if (ArchiveHelper.IsArchive(archiveFile))
					extractedFiles = Directory.GetFiles(archiveFile + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*",
						SearchOption.AllDirectories);

				logs.AddRange(extractedFiles.Select(s => GetLog(session, s, supplierId, mimeMessage)).Where(l => l != null));
			}
			return logs;
		}

		private DocumentReceiveLog GetLog(ISession session, string file, uint supplierId, MimeMessage mimeMessage)
		{
			uint addressId = 0;

			//Распарсить, получить значение адреса, который связан с клиентом (необходимо для корректного проведения накладной)
			var parserType =
				new WaybillFormatDetector().GetSuitableParserByGroup(file, WaybillFormatDetector.SuitableParserGroups.Sst)
					.FirstOrDefault();
			if (parserType == null)
				return null;
			var constructor = parserType.GetConstructors().FirstOrDefault(c => c.GetParameters().Count() == 0);
			if (constructor == null)
				throw new Exception("Не найден парсер на этапе создание логов: у типа {0} нет конструктора без аргументов.");
			var parser = (IDocumentParser) constructor.Invoke(new object[0]);

			var document = new Document();
			var doc = parser.Parse(file, document);

			if (doc?.Invoice?.RecipientId == null) {
				SendPublicErrorMessage($"В разобранном документе {file} не заполнено поле RecipientId для поставщика {supplierId}.", mimeMessage);
				_logger.InfoFormat($"В разобранном документе {file} не заполнено поле RecipientId для поставщика {supplierId}.");
				return null;
			}
			var result = session.Connection.Query<uint?>(@"
				select ai.AddressId
				from Customers.Intersection i
				join Customers.AddressIntersection ai on ai.IntersectionId = i.Id
				join Usersettings.Pricesdata pd on pd.PriceCode = i.PriceId
				join Customers.Suppliers s on s.Id = pd.FirmCode
				where  ai.SupplierDeliveryId = @supplierDeliveryId
				 and s.Id  = @supplierId
				group by ai.AddressId", new {@supplierDeliveryId = doc.Invoice.RecipientId, @supplierId = supplierId})
				.FirstOrDefault();

			if (result.HasValue)
				addressId = result.Value;

			if (addressId == 0) {
				return null;
			}


			_logger.InfoFormat($"{nameof(MailKitClient)}: обработка файла {file}");
			return DocumentReceiveLog.LogNoCommit(supplierId, addressId, file, DocType.Waybill, "Получен по Email",
				Convert.ToInt32(CurrentMesdsageId.Id));
		}

	}
}