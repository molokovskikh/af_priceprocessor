using System;
using System.Data;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;
using System.Text;
using System.Net.Mail;
using Inforoom.PriceProcessor.Properties;
using System.Configuration;


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

		protected static string GetDescription(PriceFields value)
		{
			object[] descriptions = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
			return ((System.ComponentModel.DescriptionAttribute)descriptions[0]).Description;
		}

		public override void Open()	
		{
			StringBuilder sb = new StringBuilder();

			foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields)))
				if ((pf != PriceFields.OriginalName) && !String.IsNullOrEmpty(GetFieldName(pf)) && !dtPrice.Columns.Contains(GetFieldName(pf)))
					sb.AppendFormat("\"{0}\" настроено на {1}\n", GetDescription(pf), GetFieldName(pf));

			foreach (CoreCost cost in currentCoreCosts)
				if (!String.IsNullOrEmpty(cost.fieldName) && !dtPrice.Columns.Contains(cost.fieldName))
					sb.AppendFormat("ценовая колонка \"{0}\" настроена на {1}\n", cost.costName, cost.fieldName);

			if (sb.Length > 0)
				SendAlertToUserFail(
					sb,
					"PriceProcessor: В файле отсутствуют настроенные поля",
					@"
Здравствуйте!
  В прайс-листе {0} поставщика {1} отсутствуют настроенные поля.
  Следующие поля отсутствуют:
{2}

С уважением,
  PriceProcessor.");

		}

		public override string GetFieldValue(PriceFields PF)
		{
			string res = null;

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
