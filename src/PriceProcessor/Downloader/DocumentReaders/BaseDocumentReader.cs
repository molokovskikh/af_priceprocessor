using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;

namespace Inforoom.Downloader.DocumentReaders
{
	public abstract class BaseDocumentReader
	{
		protected string[] excludeExtentions;

		/// <summary>
		/// Из двух файлов получает список клиентов, для которых надо транспортировать накладные.
		/// Если список клиентов получить не удалось, то будет вызванно исключение
		/// </summary>
		/// <param name="Connection">соединение</param>
		/// <param name="ArchFileName">код поставщика</param>
		/// <param name="ArchFileName">имя файла-архива</param>
		/// <param name="CurrentFileName">имя файла в архиве</param>
		/// <returns>список сопоставленных клиентов</returns>
		public abstract List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName);

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

		protected string GetFilterSQLHeader()
		{
			return
				@"
SELECT
	i.ClientCode AS AddressId,
	i.FirmClientCode AS SupplierClientId,
	i.FirmClientCode2 AS SupplierDeliveryId,
	i.FirmClientCode3 AS SupplierPaymentId
FROM
	usersettings.Intersection i,
	usersettings.PricesData pd
WHERE
	i.PriceCode = pd.PriceCode
	AND pd.FirmCode = ?SupplierId";
		}

		protected string GetFilterSQLFooter()
		{
			return @"
GROUP BY AddressId";
		}

		protected string SqlGetClientAddressId(bool useUnion, bool filterBySupplierClientId, bool filterBySupplierDeliveryId)
		{
			var sqlSupplierClientId = String.Empty;
			var sqlSupplierDeliveryId = String.Empty;
			var sqlUnion = String.Empty;
			if (useUnion)
				sqlUnion = " UNION ";

			if (filterBySupplierClientId)
				sqlSupplierClientId = " FutureInter.SupplierClientId = ?SupplierClientId ";
			if (filterBySupplierDeliveryId)
				sqlSupplierDeliveryId = " AddrInter.SupplierDeliveryId = ?SupplierDeliveryId ";

			var sqlCondition = sqlSupplierClientId;
			if (filterBySupplierClientId && filterBySupplierDeliveryId)
				sqlCondition = String.Format(" {0} AND {1} ", sqlSupplierClientId, sqlSupplierDeliveryId);
			else
				sqlCondition += sqlSupplierDeliveryId;

			var sqlQuery = sqlUnion + @"
SELECT
	IF (Addr.LegacyId IS NULL, Addr.Id, Addr.LegacyId) as AddressId,
	FutureInter.SupplierClientId,
	AddrInter.SupplierDeliveryId,
	FutureInter.SupplierPaymentId
FROM
	future.Addresses Addr
JOIN future.AddressIntersection AddrInter ON AddrInter.AddressId = Addr.Id
JOIN usersettings.PricesData pd ON pd.FirmCode = ?SupplierId
JOIN future.Intersection FutureInter ON FutureInter.Id = AddrInter.IntersectionId AND FutureInter.PriceId = pd.PriceCode
WHERE
" + sqlCondition;
			return sqlQuery;
		}

		public string[] ExcludeExtentions
		{
			get
			{
				return excludeExtentions;
			}
		}

		//Формируем файл для отдачи его клиенту в качестве документа
		//Потом это будет отдельный класс
		public virtual string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return InputFile;
		}

		public virtual void ImportDocument(MySqlConnection Connection, ulong FirmCode, ulong ClientCode, int DocumentType, string CurrentFileName)
		{ 
		}
	
	}
}
