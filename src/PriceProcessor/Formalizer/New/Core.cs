using System;
using System.Data;
using System.Globalization;
using Common.Tools;
using Inforoom.Formalizer;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class Cost
	{
		public decimal Value;
		public uint RequestRatio;
		public decimal MinOrderSum;
		public uint MinOrderCount;

		public CostDescription Description;

		public Cost()
		{
		}

		public Cost(CostDescription description)
		{
			Description = description;
		}

		public Cost(CostDescription description, decimal cost)
		{
			Description = description;
			Value = cost;
		}

		public static bool IsZeroOrLess(decimal cost)
		{
			return (cost < 0 || Math.Abs(Decimal.Zero - cost) < 0.01m);
		}

		public static decimal Parse(object value)
		{
			if (value == null)
				return 0;
			var stringValue = value.ToString();

			if (String.IsNullOrEmpty(stringValue))
				return 0;

			var format = CultureInfo.CurrentCulture.NumberFormat;
			var result = String.Empty;
			foreach (var charValue in stringValue) {
				if (Char.IsDigit(charValue))
					result = String.Concat(result, charValue);
				else if (!Char.IsWhiteSpace(charValue) && result != String.Empty && result.IndexOf(format.CurrencyDecimalSeparator) == -1)
					result = String.Concat(result, format.CurrencyDecimalSeparator);
			}

			//Если результирующая строка пуста, то возвращаем DBNull
			if (String.IsNullOrEmpty(result))
				return 0;

			var resultValue = Decimal.Parse(result, NumberStyles.Currency);
			resultValue = Math.Round(resultValue, 6);
			return resultValue;
		}

		public bool IsValid()
		{
			return Description != null && Value > 0;
		}
	}

	public class CostDescription
	{
		public CostDescription()
		{
		}

		public CostDescription(DataRow row)
		{
			Id = Convert.ToUInt32(row["CostCode"]);
			Name = (string)row["CostName"];
			IsBaseCost = ("1" == row["BaseCost"].ToString());
			FieldName = (string)row["FieldName"];
			Begin = (row["TxtBegin"] is DBNull) ? -1 : Convert.ToInt32(row["TxtBegin"]);
			End = (row["TxtEnd"] is DBNull) ? -1 : Convert.ToInt32(row["TxtEnd"]);
		}

		public uint Id { get; set; }
		public string Name { get; set; }
		public bool IsBaseCost { get; set; }
		public string FieldName { get; set; }
		public int Begin { get; set; }
		public int End { get; set; }

		public int UndefinedCostCount;
		public int ZeroCostCount;
	}

	public class NewCore : Core
	{
		private ExistsCore _existsCore;
		public int QuantityAsInt = -1;
		public DataRow CreatedProducerSynonym;

		public ExistsCore ExistsCore
		{
			get { return _existsCore; }
			set
			{
				_existsCore = value;
				if (_existsCore != null)
					_existsCore.NewCore = this;
			}
		}
	}

	public class ExistsCore : Core
	{
		public ulong Id;

		public NewCore NewCore;
	}

	//Не использую nullable что бы экономить память
	//для числовых полей 0 обозначает null
	public abstract class Core
	{
		public uint ProductId;
		public uint CodeFirmCr;
		public uint SynonymCode;
		public uint SynonymFirmCrCode;

		public string Code;
		public string CodeCr;
		public string Unit;
		public string Volume;
		public string Quantity;
		public string Note;
		public string Period;
		public DateTime Exp;
		public string Doc;
		public decimal RegistryCost;
		public decimal ProducerCost;
		public bool Junk;
		public bool Await;
		public bool VitallyImportant;
		public uint Nds;

		public decimal MinBoundCost;
		public decimal MaxBoundCost;

		public uint RequestRatio;
		public decimal OrderCost;
		public uint MinOrderCount;

		/// <summary>
		/// Код EAN-13 (штрих-код)
		/// </summary>
		public string EAN13;

		public uint CodeOKP;
		public string Series;

		/// <summary>
		/// будь бдителен может быть null
		/// </summary>
		public Cost[] Costs;
	}
}