using System;
using System.Data;
using Inforoom.PriceProcessor.Formalizer.New;
using MySql.Data.MySqlClient;
using System.Text;

namespace Inforoom.Formalizer
{
	public class InterPriceParser : BasePriceParser
	{
		public InterPriceParser(string priceFileName, MySqlConnection connection, PriceFormalizationInfo data) : base(priceFileName, connection, data)
		{
			foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields))) {
				var tmpName = (PriceFields.OriginalName == pf) ? "FName1" : "F" + pf;
				var row = data.FormRulesData.Rows[0];
				SetFieldName(pf, row[tmpName] is DBNull ? String.Empty : (string)row[tmpName]);
			}
		}

		protected static string GetDescription(PriceFields value)
		{
			var descriptions = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
			return ((System.ComponentModel.DescriptionAttribute)descriptions[0]).Description;
		}

		public override void Open()
		{
			//Проверку и отправку уведомлений производим только для загруженных прайс-листов
			if (downloaded) {
				var sb = new StringBuilder();

				foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields)))
					if ((pf != PriceFields.OriginalName) && !String.IsNullOrEmpty(GetFieldName(pf)) && !dtPrice.Columns.Contains(GetFieldName(pf)))
						sb.AppendFormat("\"{0}\" настроено на {1}\n", GetDescription(pf), GetFieldName(pf));

				foreach (CoreCost cost in currentCoreCosts)
					if (!String.IsNullOrEmpty(cost.fieldName) && !dtPrice.Columns.Contains(cost.fieldName))
						sb.AppendFormat("ценовая колонка \"{0}\" настроена на {1}\n", cost.costName, cost.fieldName);

				if (sb.Length > 0)
					Alerts.NotConfiguredAllert(sb, _info);
			}
		}

		public override string GetFieldValue(PriceFields field)
		{
			string res;

			//Специальным образом обрабатываем наименование товара, если имя содержится в нескольких полях
			if ((PriceFields.Name1 == field) || (PriceFields.OriginalName == field)) {
				res = base.GetFieldValue(field);
				try {
					if (dtPrice.Columns.IndexOf(GetFieldName(PriceFields.Name2)) > -1)
						res = UnSpace(String.Format("{0} {1}", res, RemoveForbWords(GetFieldRawValue(PriceFields.Name2))));
					if (dtPrice.Columns.IndexOf(GetFieldName(PriceFields.Name3)) > -1)
						res = UnSpace(String.Format("{0} {1}", res, RemoveForbWords(GetFieldRawValue(PriceFields.Name3))));
				}
				catch {
				}

				if (null != res && res.Length > 255) {
					res = res.Remove(255, res.Length - 255);
					res = res.Trim();
				}

				return res;
			}
			return base.GetFieldValue(field);
		}
	}
}