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

		protected string GetFilterSQLFooter()
		{
			return @"
GROUP BY AddressId";
		}
		
        protected string SqlGetClientAddressId(bool filterBySupplierClientId, bool filterBySupplierDeliveryId)
		{
			var sqlSupplierClientId = String.Empty;
			var sqlSupplierDeliveryId = String.Empty;

			if (filterBySupplierClientId)
				sqlSupplierClientId = " FutureInter.SupplierClientId = ?SupplierClientId ";
			if (filterBySupplierDeliveryId)
				sqlSupplierDeliveryId = " AddrInter.SupplierDeliveryId = ?SupplierDeliveryId ";

			var sqlCondition = sqlSupplierClientId;
			if (filterBySupplierClientId && filterBySupplierDeliveryId)
				sqlCondition = String.Format(" {0} AND {1} ", sqlSupplierClientId, sqlSupplierDeliveryId);
			else
				sqlCondition += sqlSupplierDeliveryId;

			var sqlQuery = @"
SELECT
    Addr.Id as AddressId
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

		//��������� ���� ��� ������ ��� ������� � �������� ���������
		//����� ��� ����� ��������� �����
		public virtual string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return InputFile;
		}

		public virtual void ImportDocument(DocumentReceiveLog log, string filename)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback))
			{
				log.Save();
				transaction.VoteCommit();
			}
		}
	}
}
