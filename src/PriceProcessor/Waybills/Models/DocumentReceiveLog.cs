using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
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

		public DocumentReceiveLog(DocumentReceiveLog log)
		{
			ClientCode = log.ClientCode;
			Address = log.Address;
			Supplier = log.Supplier;
			Comment = "Сконвертированный Dbf файл";
			DocumentType = log.DocumentType;
			FileName = Path.GetFileNameWithoutExtension(log.FileName) + ".dbf";
		}

		public bool FileIsLocal()
		{
			return !String.IsNullOrEmpty(_localFile);
		}

		//файл документа может быть локальным (если он прошел через PriceProcessor и лежит в temp) или пришедшим от клиента тогда он лежит на ftp
		public string GetFileName()
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
					Supplier.Name,
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

		public static DocumentReceiveLog Log(uint supplierId, uint? clientId, uint? addressId, string fileName, DocType documentType, int messageId)
		{
			return Log(supplierId, clientId, addressId, fileName, documentType, null, messageId);
		}

		public static DocumentReceiveLog Log(uint? supplierId, uint? clientId, uint? addressId, string fileName, DocType documentType, string comment = null, int? messageId = null, bool isFake = false)
		{
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var document = LogNoCommit(supplierId, clientId, addressId, fileName, documentType, comment, messageId, isFake);
				document.Save();
				scope.VoteCommit();
				return document;
			}
		}

		public static DocumentReceiveLog LogNoCommit(uint? supplierId,
			uint? clientId,
			uint? addressId,
			string fileName,
			DocType documentType,
			string comment = null,
			int? messageId = null,
			bool isFake = false)
		{
			fileName = CleanupFilename(fileName);
			var localFile = fileName;
			fileName = Path.GetFileName(fileName);
			var document = new DocumentReceiveLog {
				ClientCode = clientId,
				FileName = fileName,
				_localFile = localFile,
				DocumentType = documentType,
				Comment = comment,
				MessageUid = messageId,
				IsFake = isFake
			};
			if (File.Exists(localFile))
				document.DocumentSize = new FileInfo(localFile).Length;
			if (supplierId != null)
				document.Supplier = Supplier.Find(supplierId.Value);
			if (addressId != null && addressId != 0)
				document.Address = Address.Find(addressId.Value);
			return document;
		}

		//теоритически имя файла может содержать символы которых нет в 1251
		//windows хранит файлы как utf-16 а здесь он будет как utf-8 в базе же он будет как 1251
		//тогда при вставке эти символы заменятся на "похожие" и имя файла изменится 
		//и при последующих операциях мы его не найдем
		//для того что бы этого не произошло чистим имя файла
		private static string CleanupFilename(string fileName)
		{
			var convertedFileName = Common.FileHelper.FileNameToWindows1251(Path.GetFileName(fileName));
			if (!convertedFileName.Equals(Path.GetFileName(fileName), StringComparison.CurrentCultureIgnoreCase))
			{
				//Если результат преобразования отличается от исходного имени, то переименовываем файл
				convertedFileName = Path.Combine(Path.GetDirectoryName(fileName), convertedFileName);

				File.Move(fileName, convertedFileName);
				fileName = convertedFileName;
			}
			return fileName;
		}

		public string GetRemoteFileNameExt()
		{
			var clientDirectory = GetDocumentDir();

			if (!Directory.Exists(clientDirectory))
				Directory.CreateDirectory(clientDirectory);

			return GetRemoteFileName();
		}

		public void CopyDocumentToClientDirectory()
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
	}
}