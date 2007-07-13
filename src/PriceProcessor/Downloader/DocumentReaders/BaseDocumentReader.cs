using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace Inforoom.Downloader.DocumentReaders
{
	public abstract class BaseDocumentReader
	{
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

		public virtual string[] DivideFiles(string ExtractDir, string[] InputFiles)
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
	}
}
