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
			//mydr.Close();
			conn.Close();
		}
		public override void Open()
		{
			convertedToANSI = true;
			dbcMain.ConnectionString = String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"dBase 5.0\"", System.IO.Path.GetDirectoryName(priceFileName));
			//dbcMain.ConnectionString = String.Format("Provider=vfpoledb.1;Data Source={0};Collating Sequence=RUSSIAN", System.IO.Path.GetDirectoryName(priceFileName));
			dbcMain.Open();
			try
			{
				OleDbDataAdapter da = new OleDbDataAdapter(String.Format("select * from [{0}]", System.IO.Path.GetFileNameWithoutExtension(priceFileName)), dbcMain);
				//da.Fill(dtPrice);
				FillPrice(da);
			}
			finally
			{
				dbcMain.Close();
				dbcMain.Dispose();
			}

			CurrPos = 0;
		}
	}
}
