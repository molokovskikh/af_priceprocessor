using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;

namespace Inforoom.Formalizer
{
	public class InterPriceParser : BasePriceParser
	{
		public InterPriceParser(string priceFileName, MySqlConnection conn, DataTable mydr) : base(priceFileName, conn, mydr)
		{
			foreach(PriceFields pf in Enum.GetValues(typeof(PriceFields)))
			{
				var tmpName = (PriceFields.OriginalName == pf) ? "FName1" : "F" + pf;
				SetFieldName(pf, mydr.Rows[0][tmpName] is DBNull ? String.Empty : (string)mydr.Rows[0][tmpName]);
			}
		}

		protected static string GetDescription(PriceFields value)
		{
			var descriptions = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
			return ((System.ComponentModel.DescriptionAttribute)descriptions[0]).Description;
		}

		public override void Open()	
		{
			//�������� � �������� ����������� ���������� ������ ��� ����������� �����-������
			if (downloaded)
			{
				var sb = new StringBuilder();

				foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields)))
					if ((pf != PriceFields.OriginalName) && !String.IsNullOrEmpty(GetFieldName(pf)) && !dtPrice.Columns.Contains(GetFieldName(pf)))
						sb.AppendFormat("\"{0}\" ��������� �� {1}\n", GetDescription(pf), GetFieldName(pf));

				foreach (CoreCost cost in currentCoreCosts)
					if (!String.IsNullOrEmpty(cost.fieldName) && !dtPrice.Columns.Contains(cost.fieldName))
						sb.AppendFormat("������� ������� \"{0}\" ��������� �� {1}\n", cost.costName, cost.fieldName);

				if (sb.Length > 0)
					SendAlertToUserFail(
						sb,
						"PriceProcessor: � �����-����� {0} ���������� {1} ����������� ����������� ����",
						@"
������������!
  � �����-����� {0} ���������� {1} ����������� ����������� ����.
  ��������� ���� �����������:
{2}

� ���������,
  PriceProcessor.");
			}

		}

		public override string GetFieldValue(PriceFields PF)
		{
			string res;

			//����������� ������� ������������ ������������ ������, ���� ��� ���������� � ���������� �����
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
			return base.GetFieldValue(PF);
		}
	}
}
