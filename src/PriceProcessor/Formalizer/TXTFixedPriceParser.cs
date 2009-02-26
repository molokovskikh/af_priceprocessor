using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Collections;
using Inforoom.PriceProcessor.Formalizer;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;


namespace Inforoom.Formalizer
{

	public class TxtFieldDef : IComparer
	{
		public string fieldName;
		public int posBegin;
		public int posEnd;

		public TxtFieldDef()
		{
		}

		public TxtFieldDef(string AFieldName, int AposBegin, int AposEnd)
		{
			fieldName = AFieldName;
			posBegin = AposBegin;
			posEnd = AposEnd;
		}

		int IComparer.Compare( Object x, Object y )  
		{
			return ( ((TxtFieldDef)x).posBegin - ((TxtFieldDef)y).posBegin );
		}
	}

	/// <summary>
	/// Summary description for TXTFixedPriceParser.
	/// </summary>
	public class TXTFixedPriceParser : BasePriceParser
	{
		//строка, с которой надо разбирать прайс
		protected System.Int64 startLine;

		private ArrayList fds;

		// одировка файла
		protected string FileEncoding;

		public TXTFixedPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr) : base(PriceFileName, conn, mydr)
		{
			string TmpName;
			int TmpIndex;

			//«аполн€ем названи€ полей
			foreach(PriceFields pf in Enum.GetValues(typeof(PriceFields)))
			{
				if (PriceFields.OriginalName == pf || PriceFields.Name1 == pf)
					TmpName = "Name";
				else
					TmpName = pf.ToString();
				try
				{
					TmpIndex = mydr.Columns.IndexOf("Txt" + TmpName + "Begin");
				}
				catch
				{
					TmpIndex = -1;
				}
				SetFieldName(pf, (-1 == TmpIndex) ? String.Empty : TmpName);
			}
			startLine = mydr.Rows[0]["StartLine"] is DBNull ? -1 : Convert.ToInt64(mydr.Rows[0]["StartLine"]);

			//«аполн€ем параметры определени€ полей: название поле, поле с позицией начала и поле с позицией конца
			fds = new ArrayList();
			int TxtBegin, TxtEnd;
			foreach(PriceFields pf in Enum.GetValues(typeof(PriceFields)))
			{
				TmpName = GetFieldName(pf);
				if ((PriceFields.OriginalName != pf) && (null != TmpName))
				{
					//TODO: ѕоле может быть не заполненно, т.е. одно из полей может иметь значение NULL или пуста€ строка (String.Empty)
					try
					{
						TxtBegin = Convert.ToInt32(mydr.Rows[0]["Txt" + TmpName + "Begin"]);
						TxtEnd = Convert.ToInt32(mydr.Rows[0]["Txt" + TmpName + "End"]);
						fds.Add(new TxtFieldDef(
							TmpName, 
							TxtBegin, 
							TxtEnd
							)
						);
					}
					catch{}
				}
			}

			foreach(CoreCost cc in currentCoreCosts)
			{
				cc.fieldName = "Cost" + cc.costCode.ToString();
				fds.Add(
					new TxtFieldDef(
						cc.fieldName,
						cc.txtBegin,
						cc.txtEnd
					)
				);
			}

			if (fds.Count < 1)
				throw new WarningFormalizeException(Settings.Default.MinFieldCountError, firmCode, priceCode, firmShortName, priceName);

			//ѕроизводим сортировку полей по полую с позицией начала
			fds.Sort( new TxtFieldDef() );

			conn.Close();
		}

		public override void Open()
		{
			//‘ормируем Schema.ini дл€ распозновани€
			using(StreamWriter w = new StreamWriter(Path.GetDirectoryName(priceFileName) + Path.DirectorySeparatorChar + "Schema.ini", false, Encoding.GetEncoding(1251)))
			{
				w.WriteLine("[" + Path.GetFileName(priceFileName) + "]");
				convertedToANSI = (FileEncoding == "OEM");
				w.WriteLine("CharacterSet=" + FileEncoding);
				w.WriteLine("Format=FixedLength");
				w.WriteLine("ColNameHeader=False");
				w.WriteLine("MaxScanRows=300");

				int j = 1;
				TxtFieldDef prevTFD, currTFD = (TxtFieldDef)fds[0];

				if ( 1 == currTFD.posBegin )
				{
					w.WriteLine( String.Format("Col{0}={1} Text Width {2}", j, currTFD.fieldName, currTFD.posEnd) );
					j++;
				}
				else
				{
					w.WriteLine( String.Format("Col{0}={1} Text Width {2}", j, "x", currTFD.posBegin-1) );
					j++;
					w.WriteLine( String.Format("Col{0}={1} Text Width {2}", j, currTFD.fieldName, currTFD.posEnd - currTFD.posBegin + 1) );
					j++;
				}

				for(int i = 1; i<=fds.Count-1; i++)
				{
					prevTFD = (TxtFieldDef)fds[i-1];
					currTFD = (TxtFieldDef)fds[i];
					if (currTFD.posBegin == prevTFD.posEnd + 1)
					{
						w.WriteLine( String.Format("Col{0}={1} Text Width {2}", j, currTFD.fieldName, currTFD.posEnd - currTFD.posBegin + 1) );
						j++;
					}
					else
					{
						w.WriteLine( String.Format("Col{0}={1} Text Width {2}", j, "x", currTFD.posBegin - prevTFD.posEnd - 1) );
						j++;
						w.WriteLine( String.Format("Col{0}={1} Text Width {2}", j, currTFD.fieldName, currTFD.posEnd - currTFD.posBegin + 1) );
						j++;
					}
				}
			}

			using (OleDbConnection dbcMain = new OleDbConnection(String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Text\"", System.IO.Path.GetDirectoryName(priceFileName))))
			{
				dbcMain.Open();
				using (OleDbDataAdapter da = new OleDbDataAdapter(String.Format("select * from {0}", System.IO.Path.GetFileName(priceFileName).Replace(".", "#")), dbcMain))
				{
					dtPrice = OleDbHelper.FillPrice(da);
				}
			}

			if (startLine > 0)
				CurrPos = Convert.ToInt32(startLine);
			else
				CurrPos = 0;
		}
	}
}
