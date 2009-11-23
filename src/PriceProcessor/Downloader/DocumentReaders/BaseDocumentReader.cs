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
		/// �� ���� ������ �������� ������ ��������, ��� ������� ���� ���������������� ���������.
		/// ���� ������ �������� �������� �� �������, �� ����� �������� ����������
		/// </summary>
		/// <param name="Connection">����������</param>
		/// <param name="ArchFileName">��� ����������</param>
		/// <param name="ArchFileName">��� �����-������</param>
		/// <param name="CurrentFileName">��� ����� � ������</param>
		/// <returns>������ �������������� ��������</returns>
		public abstract List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName);

		//��������� ����� ����� ����������, ���� � ����� ����� ���������� ��������� ����������
		public virtual string[] DivideFiles(string ExtractDir, string[] InputFiles)
		{
			return InputFiles;
		}

		//���������� ����� ����� ��������� �� ������ ����������, ���� ���������� � ��������� ���������� � ���������� ������
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
				sqlSupplierClientId = " AND Inter.SupplierClientId = ?SupplierClientId ";
			if (filterBySupplierDeliveryId)
				sqlSupplierDeliveryId = " AND AddrInter.SupplierDeliveryId = ?SupplierDeliveryId ";
			var sqlQuery = sqlUnion + @"
SELECT
	IF (Addr.LegacyId IS NULL, Addr.Id, Addr.LegacyId) AS AddressId,
	Inter.SupplierClientId,
	AddrInter.SupplierDeliveryId,
	Inter.SupplierPaymentId
FROM
	future.Addresses Addr,
	future.Intersection Inter,
	future.AddressIntersection AddrInter,
	usersettings.PricesData pd
WHERE
	Inter.PriceId = pd.PriceCode
AND pd.FirmCode = ?SupplierId
" + sqlSupplierClientId + sqlSupplierDeliveryId + 
@"
AND Addr.Id = AddrInter.AddressId";

			return sqlQuery;
		}

		public string[] ExcludeExtentions
		{
			get
			{
				return excludeExtentions;
			}
		}

		//��������� ���� ��� ������ ��� ������� � �������� ���������
		//����� ��� ����� ��������� �����
		public virtual string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return InputFile;
		}

		public virtual void ImportDocument(MySqlConnection Connection, ulong FirmCode, ulong ClientCode, int DocumentType, string CurrentFileName)
		{ 
		}
	
	}
}
