using System;
using System.Data;
using System.Data.OleDb;
using MySql.Data.MySqlClient;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for DBFPriceParser.
	/// </summary>
	public class DBFPriceParser : InterPriceParser
	{
		public DBFPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr) : base(PriceFileName, conn, mydr)
		{
			conn.Close();
		}
		public override void Open()
		{
			convertedToANSI = true;
			using (OleDbConnection dbcMain = new OleDbConnection(String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"dBase 5.0\"", System.IO.Path.GetDirectoryName(priceFileName))))
			{
				dbcMain.Open();
				using (OleDbDataAdapter da = new OleDbDataAdapter(String.Format("select * from [{0}]", System.IO.Path.GetFileNameWithoutExtension(priceFileName)), dbcMain))
				{
					FillPrice(da);
				}
			}

			CurrPos = 0;

			base.Open();
		}
	}
}
