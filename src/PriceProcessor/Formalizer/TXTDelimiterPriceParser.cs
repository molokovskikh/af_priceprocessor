using System;
using System.IO;
using System.Data;
using System.Text;
using System.Data.OleDb;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for TXTDelimiterPriceParser.
	/// </summary>
	public class TXTDelimiterPriceParser : InterPriceParser
	{
		//строка, с которой надо разбирать прайс
		protected System.Int64 startLine;
		//Разделитель между полями
		protected string delimiter;

		//Кодировка файла
		protected string FileEncoding;

		public TXTDelimiterPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr) : base(PriceFileName, conn, mydr)
		{
			delimiter = mydr.Rows[0][FormRules.colDelimiter].ToString();
			startLine = mydr.Rows[0]["StartLine"] is DBNull ? -1 : Convert.ToInt64(mydr.Rows[0]["StartLine"]);
			conn.Close();
		}

		public override void Open()
		{
			convertedToANSI = (FileEncoding == "OEM");
			using(StreamWriter w = new StreamWriter(Path.GetDirectoryName(priceFileName) + Path.DirectorySeparatorChar + "Schema.ini", false, Encoding.GetEncoding(1251)))
			{
                w.WriteLine("[" + Path.GetFileName(priceFileName) + "]");
				w.WriteLine("CharacterSet=" + FileEncoding);
				w.WriteLine(("TAB" == delimiter.ToUpper()) ? "Format=TabDelimited" : "Format=Delimited(" + delimiter + ")");
				w.WriteLine("ColNameHeader=False");
				w.WriteLine("MaxScanRows=300");
			}

			string replaceFile;
			using (StreamReader r = new StreamReader(priceFileName, Encoding.GetEncoding(1251)))
			{
				replaceFile = r.ReadToEnd();
			}

			replaceFile = replaceFile.Replace("\"", "");

			using (StreamWriter rw = new StreamWriter(priceFileName, false, Encoding.GetEncoding(1251)))
			{
				rw.Write(replaceFile);
			}

			int MaxColCount = 0;
			string TableName = System.IO.Path.GetFileName(priceFileName).Replace(".", "#");
			dbcMain.ConnectionString = String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Text\"", System.IO.Path.GetDirectoryName(priceFileName));
			dbcMain.Open();
			try
			{
				DataTable ColumnNames = dbcMain.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
					new object[] {null, null, TableName, null});
				MaxColCount = (ColumnNames.Rows.Count >= 256) ? 255 : ColumnNames.Rows.Count;
			}
			finally
			{
				dbcMain.Close();
				dbcMain.Dispose();
			}

			using(StreamWriter w = new StreamWriter(Path.GetDirectoryName(priceFileName) + Path.DirectorySeparatorChar + "Schema.ini", false, Encoding.GetEncoding(1251)))
			{
				w.WriteLine("[" + Path.GetFileName(priceFileName) + "]");
				w.WriteLine("CharacterSet=" + FileEncoding);
				w.WriteLine(("TAB" == delimiter.ToUpper()) ? "Format=TabDelimited" : "Format=Delimited(" + delimiter + ")");
				w.WriteLine("ColNameHeader=False");
				w.WriteLine("MaxScanRows=300");
				for(int i = 0;i<=MaxColCount;i++)
				{
					w.WriteLine("Col{0}=F{0} Text", i);
				}
			}

			dbcMain.ConnectionString = String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Text\"", System.IO.Path.GetDirectoryName(priceFileName));
			dbcMain.Open();
			try
			{
				OleDbDataAdapter da = new OleDbDataAdapter(String.Format("select * from {0}", System.IO.Path.GetFileName(priceFileName).Replace(".", "#")), dbcMain);
				FillPrice(da);
			}
			finally
			{
				dbcMain.Close();
				dbcMain.Dispose();
			}

			if (startLine > 0)
				CurrPos = Convert.ToInt32(startLine);
			else
				CurrPos = 0;
		}
	}
}
