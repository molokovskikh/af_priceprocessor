using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;
using NHibernate;
using log4net;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("Document_logs", Schema = "logs")]
	public class DocumentReceiveLog : ActiveRecordLinqBase<DocumentReceiveLog>
	{
		private static readonly ILog _logger = LogManager.GetLogger(typeof (DocumentReceiveLog));

		[PrimaryKey("RowId")]
		public uint Id { get; set; }

		[BelongsTo("FirmCode")]
		public Supplier Supplier { get; set; }

		[Property(Insert = false, Update = false)]
		public DateTime LogTime { get; set; }

		[Property]
		public uint? ClientCode { get; set; }

		[BelongsTo("AddressId")]
		public Address Address { get; set; }

		[Property]
		public DocType DocumentType { get; set; }

		[Property("Addition")]
		public string Comment { get; set; }

		[Property]
		public string FileName { get; set; }

		[Property]
		public int? MessageUid { get; set; }

		[Property]
		public long? DocumentSize { get; set; }

		[Property]
		public bool IsFake { get; set; }

		private string _localFile;

		public DocumentReceiveLog()
		{}

		public DocumentReceiveLog(DocumentReceiveLog log, string extention)
		{
			ClientCode = log.ClientCode;
			Address = log.Address;
			Supplier = log.Supplier;
			Comment = "Сконвертированный файл";
			DocumentType = log.DocumentType;
			FileName = Path.GetFileNameWithoutExtension(log.FileName) + extention;
		}

		public DocumentReceiveLog(Supplier supplier, Address address)
		{
			Supplier = supplier;
			Address = address;
			if (address != null)
				ClientCode = address.Client.Id;
		}

		public virtual bool FileIsLocal()
		{
			return !String.IsNullOrEmpty(_localFile);
		}

		//файл документа может быть локальным (если он прошел через PriceProcessor и лежит в temp) или пришедшим от клиента тогда он лежит на ftp
		public virtual string GetFileName()
		{
			if (!String.IsNullOrEmpty(_localFile))
				return _localFile;

			return GetRemoteFileName();
		}

		private string GetRemoteFileName()
		{
			var documentDir = GetDocumentDir();
			var file = String.Format("{0}_{1}",
				Id,
				Path.GetFileName(FileName));
			var fullName = Path.Combine(documentDir, file);
			if (!File.Exists(fullName))
			{
				file = String.Format("{0}_{1}({2}){3}",
					Id,
					global::Common.Tools.FileHelper.FileNameToWindows1251(Supplier.Name),
					Path.GetFileNameWithoutExtension(FileName),
					Path.GetExtension(FileName));
				return Path.Combine(documentDir, file);
			}
			return fullName;
		}

		private string GetDocumentDir()
		{
			if (Address == null)
				throw new Exception("Не могу получить путь документа для неизвестного адреса доставки");

			var clientDir = Path.Combine(Settings.Default.DocumentPath, Address.Id.ToString().PadLeft(3, '0'));
			return Path.Combine(clientDir, DocumentType + "s");
		}

		public static DocumentReceiveLog Log(uint? supplierId,
			uint? addressId,
			string fileName,
			DocType documentType,
			string comment = null,
			int? messageId = null,
			bool isFake = false)
		{
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var document = LogNoCommit(supplierId, addressId, fileName, documentType, comment, messageId, isFake);
				document.Save();
				scope.VoteCommit();
				return document;
			}
		}

		public static DocumentReceiveLog LogNoCommit(uint? supplierId,
			uint? addressId,
			string fileName,
			DocType documentType,
			string comment = null,
			int? messageId = null,
			bool isFake = false)
		{
			using(new SessionScope()) {
				fileName = CleanupFilename(fileName);
				var localFile = fileName;
				fileName = Path.GetFileName(fileName);

				Supplier supplier = null;
				if (supplierId != null)
					supplier = Supplier.Find(supplierId.Value);

				Address address = null;
				if (addressId != null) {
					address = Address.TryFind(addressId.Value);
					if (address != null) {
						NHibernateUtil.Initialize(address);
						NHibernateUtil.Initialize(address.Org);
						NHibernateUtil.Initialize(address.Client);
					}
				}
				var document = new DocumentReceiveLog(supplier, address) {
					FileName = fileName,
					_localFile = localFile,
					DocumentType = documentType,
					Comment = comment,
					MessageUid = messageId,
					IsFake = isFake
				};
				if (File.Exists(localFile))
					document.DocumentSize = new FileInfo(localFile).Length;
				return document;
			}
		}

		//теоритически имя файла может содержать символы которых нет в 1251
		//windows хранит файлы как utf-16 а здесь он будет как utf-8 в базе же он будет как 1251
		//тогда при вставке эти символы заменятся на "похожие" и имя файла изменится 
		//и при последующих операциях мы его не найдем
		//для того что бы этого не произошло чистим имя файла
		private static string CleanupFilename(string fileName)
		{
			var convertedFileName = global::Common.Tools.FileHelper.FileNameToWindows1251(Path.GetFileName(fileName));
			if (!convertedFileName.Equals(Path.GetFileName(fileName), StringComparison.CurrentCultureIgnoreCase))
			{
				//Если результат преобразования отличается от исходного имени, то переименовываем файл
				convertedFileName = Path.Combine(Path.GetDirectoryName(fileName), convertedFileName);

				File.Copy(fileName, convertedFileName, true);
				fileName = convertedFileName;
			}
			return fileName;
		}

		public virtual string GetRemoteFileNameExt()
		{
			var clientDirectory = GetDocumentDir();

			if (!Directory.Exists(clientDirectory))
				Directory.CreateDirectory(clientDirectory);

			return GetRemoteFileName();
		}

		public virtual void CopyDocumentToClientDirectory()
		{
			var destinationFileName = GetRemoteFileNameExt();

			//Проверяем, если у нас файл не локальный, то он уже лежит в destinationFileName и _localFile = null.
			if (GetFileName() != destinationFileName)
				File.Copy(_localFile, destinationFileName, true);

			//вроде бы это не нужно и все происходит автоматически но на всякий случай
			//нужно что бы последнее обращение было актуальныс что бы удалять на сервере старые файлы
			File.SetLastAccessTime(destinationFileName, DateTime.Now);

			if (_logger.IsDebugEnabled)
				WaybillService.SaveWaybill(_localFile);
		}

		public static List<DocumentReceiveLog> LoadByIds(uint[] ids)
		{
			return ids.Select(id => Find(id)).ToList();
		}

		public virtual void Check()
		{
			if (!Address.Enabled || !Address.Client.Enabled)
				throw new EMailSourceHandlerException(
					String.Format("Адрес доставки {0} отключен", Address.Id),
					"Ваше Сообщение не доставлено одной или нескольким аптекам",
					"Добрый день.\r\n\r\n"
					+ "Документы (накладные, отказы) в Вашем Сообщении с темой: \"{0}\" не были доставлены аптеке, т.к. аптека отключена в рамках системы АналитФармация.\r\n\r\n"
					+ "С уважением, АК \"Инфорум\".");

			if ((Address.Client.MaskRegion & Supplier.RegionMask) == 0)
				throw new EMailSourceHandlerException(
					String.Format("Адрес доставки {0} не доступен поставщику {1}", Address.Id, Supplier.Id),
					"Ваше Сообщение не доставлено одной или нескольким аптекам",
					"Добрый день.\r\n\r\n"
					+ "Документы (накладные, отказы) в Вашем Сообщении с темой: \"{0}\" не были доставлены аптеке, т.к. указанный адрес получателя не соответствует ни одной из работающих аптек в регионе(-ах) Вашей работы.\r\n\r\n"
					+ "Пожалуйста, проверьте корректность указания адреса аптеки и, после исправления, отправьте документы вновь.\r\n\r\n"
					+ "С уважением, АК \"Инфорум\".");
		}
	}

	public class NotificationException : Exception
	{}
}