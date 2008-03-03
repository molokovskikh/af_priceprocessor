using System;
using System.Data;
using System.Data.OleDb;
using MySql.Data.MySqlClient;
using Inforoom.Logging;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for ExcelPriceParser.
	/// </summary>
	public class ExcelPriceParser : InterPriceParser
	{
		//название листа, с которого надо читать прайс
		protected string listName;
		//строка, с которой надо разбирать прайс
		protected System.Int64 startLine;

		//Кол-во пустых строк
		//TODO: Корректно рассчитать кол-во пустых строк в файле, чтобы выставить окончание
		protected int blackCount;

		public ExcelPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr) : base(PriceFileName, conn, mydr)
		{
			listName = mydr.Rows[0]["ListName"].ToString();
			startLine = mydr.Rows[0]["StartLine"] is DBNull ? -1 : Convert.ToInt64(mydr.Rows[0]["StartLine"]);
			//mydr.Close();
			conn.Close();
		}

		public override void Open()
		{
			convertedToANSI = true;
			dbcMain.ConnectionString = String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Excel 5.0;HDR=No;IMEX=1\"", priceFileName);
			bool res = false;
			int tryCount = 0;
			do
			{
				try
				{
					dbcMain.Open();
					try
					{
						DataTable TableNames = dbcMain.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
							new object[] {null, null, null, "TABLE"});
						if (0 == TableNames.Rows.Count)
							throw new WarningFormalizeException(Settings.Default.SheetsNotExistsError, clientCode, priceCode, clientShortName, priceName);
						string Sheet = null;
						foreach(DataRow dr in TableNames.Rows)
						{
							if (!(dr["TABLE_NAME"] is DBNull) && ((string)dr["TABLE_NAME"] == listName))
							{
								Sheet = (string)dr["TABLE_NAME"];
								break;
							}
						}
						if (null == Sheet)
							Sheet = (string)TableNames.Rows[0]["TABLE_NAME"];

						DataTable ColumnNames = dbcMain.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
							new object[] {null, null, Sheet, null});
						if (0 == ColumnNames.Rows.Count)
							throw new WarningFormalizeException(Settings.Default.FieldsNotExistsError, clientCode, priceCode, clientShortName, priceName);
						string FieldNames = "F1";
						int MaxColCount = (ColumnNames.Rows.Count >= 256) ? 255 : ColumnNames.Rows.Count;
						//todo: Поставить проверку на максимальное кол-во столбцов
						for(int i = 1;i<MaxColCount;i++)
						{
							FieldNames = FieldNames + ", F" + Convert.ToString(i+1);
						}
						OleDbDataAdapter da = new OleDbDataAdapter(String.Format("select {0} from [{1}]", FieldNames, Sheet), dbcMain);
						//da.Fill(dtPrice);
						FillPrice(da);
					}
					finally
					{
						dbcMain.Close();
					}
					res = true;
				}
				catch(System.Data.OleDb.OleDbException)
				{
					if (tryCount < Settings.Default.MinRepeatTranCount)
					{
						tryCount++;
						SimpleLog.Log( getParserID(), "Repeat dbcMain.Open on OleDbException");
						System.Threading.Thread.Sleep(500);
					}
					else
						throw;
				}
				catch(System.Runtime.InteropServices.InvalidComObjectException)
				{
					if (tryCount < Settings.Default.MinRepeatTranCount)
					{
						tryCount++;
						SimpleLog.Log( getParserID(), "Repeat dbcMain.Open on InvalidComObjectException");
						System.Threading.Thread.Sleep(500);
					}
					else
						throw;
				}
			}while(!res);

			if (startLine > 0)
				CurrPos = Convert.ToInt32(startLine);
			else
				CurrPos = 0;

			blackCount = 0;

		}

		public override bool Next()
		{
			bool res = base.Next();

			if (res)
			{
				if (String.Empty == GetFieldRawValue(PriceFields.Name1).Trim())
					blackCount++;
				else
					blackCount = 0;
				if (30 == blackCount)
				{
					CurrPos--;
					res = false;
				}
			}
				
			return res;
		}

	}
}
