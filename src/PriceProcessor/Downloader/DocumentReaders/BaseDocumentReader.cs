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
			return @"SELECT
  cd.firmcode as ClientCode,
  IncludeRegulation.IncludeType,
  i.FirmClientCode,
  i.FirmClientCode2,
  i.FirmClientCode3
FROM
  (usersettings.clientsdata as cd,
   usersettings.intersection i,
   usersettings.pricesdata pd)
   LEFT JOIN usersettings.includeregulation
        ON includeclientcode= cd.firmcode
WHERE
  i.clientCode = cd.FirmCode
  and i.PriceCode = pd.PriceCode
  and pd.FirmCode = ?FirmCode
  and if(IncludeRegulation.PrimaryClientCode is null, 1, IncludeRegulation.IncludeType <>2)";
		}

		protected string GetFilterSQLFooter()
		{
			return "group by cd.firmcode";
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
	}
}
