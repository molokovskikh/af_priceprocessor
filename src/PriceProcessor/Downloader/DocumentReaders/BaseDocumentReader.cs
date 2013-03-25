using System;
using System.Collections.Generic;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using System.Data;

namespace Inforoom.Downloader.DocumentReaders
{
	public abstract class BaseDocumentReader
	{
		protected string[] excludeExtentions = new string[0];

		/// <summary>
		/// Из двух файлов получает список клиентов, для которых надо транспортировать накладные.
		/// Если список клиентов получить не удалось, то будет вызванно исключение
		/// </summary>
		/// <param name="Connection">соединение</param>
		/// <param name="ArchFileName">код поставщика</param>
		/// <param name="ArchFileName">имя файла-архива</param>
		/// <param name="CurrentFileName">имя файла в архиве</param>
		/// <returns>список сопоставленных клиентов</returns>
		public abstract List<ulong> ParseAddressIds(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName);

		//Разделяем файлы перед обработкой, если в одном файле содержится несколько документов
		public virtual string[] DivideFiles(string ExtractDir, string[] InputFiles)
		{
			return InputFiles;
		}

		//Объединяем файлы после получения из списка источников, если информация о документе содержится в нескольких файлах
		public virtual string[] UnionFiles(string[] InputFiles)
		{
			return InputFiles;
		}

		public string[] ExcludeExtentions
		{
			get { return excludeExtentions; }
		}

		//Формируем файл для отдачи его клиенту в качестве документа
		//Потом это будет отдельный класс
		public virtual string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return InputFile;
		}

		public virtual void ImportDocument(DocumentReceiveLog log, string filename)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				log.Save();
				transaction.VoteCommit();
			}
		}
	}
}