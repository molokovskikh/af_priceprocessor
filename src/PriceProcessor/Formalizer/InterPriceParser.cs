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
			{
				DataRow drProvider = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(), @"
select
  if(pd.CostType = 1, concat('[Колонка] ', pc.CostName), pd.PriceName) PriceName,
  concat(cd.ShortName, ' - ', r.Region) ShortFirmName
from
usersettings.pricescosts pc,
usersettings.pricesdata pd,
usersettings.clientsdata cd,
farm.regions r
where
    pc.PriceItemId = ?PriceItemId
and pd.PriceCode = pc.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.FirmCode = pd.FirmCode
and r.RegionCode = cd.RegionCode",
								 new MySqlParameter("?PriceItemId", priceItemId));
				string subject = "PriceProcessor: В файле отсутствуют настроенные поля";
				string body = String.Format(@"
Здравствуйте!
  В прайс-листе {0} поставщика {1} отсутствуют настроенные поля.
  Следующие поля отсутствуют:
{2}

С уважением,
  PriceProcessor.",
				  drProvider["PriceName"],
				  drProvider["ShortFirmName"],
				  sb.ToString());

				using (MailMessage m = new MailMessage(Settings.Default.ServiceMail, Settings.Default.SMTPUserFail, subject, body))
				{
					SmtpClient client = new SmtpClient(Settings.Default.SMTPHost);
					client.Send(m);
				}
			}

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
