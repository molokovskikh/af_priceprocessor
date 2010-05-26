using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Inforoom.PriceProcessor.Properties;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	[ActiveRecord("Document_logs", Schema = "logs")]
	public class DocumentReceiveLog : ActiveRecordLinqBase<DocumentReceiveLog>
	{
		private static readonly ILog _logger = LogManager.GetLogger(typeof (DocumentReceiveLog));

		[PrimaryKey("RowId")]
		public uint Id { get; set; }

		[BelongsTo("FirmCode")]
		public Supplier Supplier { get; set; }

		[Property]
		public uint? ClientCode { get; set; }

		[Property]
		public uint? AddressId { get; set; }

		[Property]
		public DocType DocumentType { get; set; }

		[Property("Addition")]
		public string Comment { get; set; }

		[Property]
		public string FileName { get; set; }

		[Property]
		public int? MessageUid { get; set; }

		private string _localFile;

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
					Supplier.ShortName,
					Path.GetFileNameWithoutExtension(FileName),
					Path.GetExtension(FileName));
				return Path.Combine(documentDir, file);
			}
			return fullName;
		}

		private string GetDocumentDir()
		{
			var code = AddressId.HasValue ? AddressId.Value : ClientCode;
			var clientDir = Path.Combine(Settings.Default.WaybillsPath, code.ToString().PadLeft(3, '0'));
			return Path.Combine(clientDir, DocumentType + "s");
		}

		public static DocumentReceiveLog Log(uint supplierId, uint? clientId, uint? addressId, string fileName, DocType documentType)
		{
			return Log(supplierId, clientId, addressId, fileName, documentType, null, null);
		}

		public static DocumentReceiveLog Log(uint supplierId, uint? clientId, uint? addressId, string fileName, DocType documentType, int messageId)
		{
			return Log(supplierId, clientId, addressId, fileName, documentType, null, messageId);
		}

		public static DocumentReceiveLog Log(uint supplierId, uint? clientId, uint? addressId, string fileName, DocType documentType, string comment, int? messageId)
		{
			fileName = CleanupFilename(fileName);
			var localFile = fileName;
			fileName = Path.GetFileName(fileName);
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var supplier = Supplier.Find(supplierId);
				var document = new DocumentReceiveLog {
					Supplier = supplier,
					AddressId = addressId,
					ClientCode = clientId,
					FileName = fileName,
					_localFile = localFile,
					DocumentType = documentType,
					Comment = comment,
					MessageUid = messageId
				};
				document.Create();
				scope.VoteCommit();
				return document;
			}
		}

		
		public static void LogFail(uint supplierId, uint? clientId, uint? clientAddressId, DocType docType, string filename, string message)
		{
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var supplier = Supplier.Find(supplierId);
				var document = new DocumentReceiveLog {
					Supplier = supplier,
					AddressId = clientAddressId,
					ClientCode = clientId,
					FileName = filename,
					DocumentType = docType,
					Comment = message,
				};
				document.Create();
				scope.VoteCommit();
			}
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

		public void CopyDocumentToClientDirectory()
		{
			var clientDirectory = GetDocumentDir();

			if (!Directory.Exists(clientDirectory))
				Directory.CreateDirectory(clientDirectory);

			var destinationFileName = GetRemoteFileName();

			//вроде бы это не нужно и все происходит автоматически но на всякий случай
			//нужно что бы последнее обращение было актуальныс что бы удалять на сервере старые файлы
			File.SetLastAccessTime(_localFile, DateTime.Now);
			File.Copy(_localFile, destinationFileName, true);

			if (_logger.IsDebugEnabled)
				WaybillService.SaveWaybill(_localFile);
		}

		public static List<DocumentReceiveLog> LoadByIds(uint[] ids)
		{
			return ids.Select(id => Find(id)).ToList();
		}
	}
}