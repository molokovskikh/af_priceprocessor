using System;
using System.Data;
using MySql.Data.MySqlClient;


namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for InterPriceParser.
	/// </summary>
	public class InterPriceParser : BasePriceParser
	{
		public InterPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr) : base(PriceFileName, conn, mydr)
		{
			string TmpName;
			foreach(PriceFields pf in Enum.GetValues(typeof(PriceFields)))
			{
				TmpName = (PriceFields.OriginalName == pf) ? "FName1" : "F" + pf.ToString();
				SetFieldName(pf, mydr.Rows[0][TmpName] is DBNull ? String.Empty : (string)mydr.Rows[0][TmpName]);
			}

		}

		public override void Open()	
		{
		}

		public override string GetFieldValue(PriceFields PF)
		{
			string res = null;

			//TODO: Проверить, правильно ли формируется оригинальное имя
			//Специальным образом обрабатываем наименование товара, если имя содержится в нескольких полях
			if ((PriceFields.Name1 == PF) || (PriceFields.OriginalName == PF)) 
			{
				res = base.GetFieldValue(PF);
				try
				{
					if (dtPrice.Columns.IndexOf( GetFieldName(PriceFields.Name2) ) > -1)
						res = UnSpace( String.Format("{0} {1}", res, RemoveForbWords( GetFieldRawValue(PriceFields.Name2) ) ) );
					if (dtPrice.Columns.IndexOf( GetFieldName(PriceFields.Name3) ) > -1)
						res = UnSpace( String.Format("{0} {1}", res, RemoveForbWords( GetFieldRawValue(PriceFields.Name3) ) ) );
				}
				catch
				{
				}

				if (null != res && res.Length > 255)
				{
					res = res.Remove(255, res.Length - 255);
					res = res.Trim();
				}

				return res;
			}
			else
				return base.GetFieldValue(PF);
		}


	}
}
