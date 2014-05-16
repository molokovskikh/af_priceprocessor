using System;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class WarningFormalizeException : FormalizeException
	{
		public WarningFormalizeException(string message) : base(message)
		{
		}

		public WarningFormalizeException(string message, PriceFormalizationInfo info) : base(message, info)
		{
		}

		public WarningFormalizeException(string message, Int64 ClientCode,
			Int64 PriceCode, string ClientName, string PriceName)
			: base(message, ClientCode, PriceCode, ClientName, PriceName)
		{
		}
	}

	public class FormalizeException : Exception
	{
		public readonly long clientCode = -1;
		public readonly long priceCode = -1;
		public readonly string clientName = "";
		public readonly string priceName = "";

		public FormalizeException(string message) : base(message)
		{
		}

		public FormalizeException(string message, PriceFormalizationInfo priceInfo)
			: base(message)
		{
			clientCode = priceInfo.FirmCode;
			priceCode = priceInfo.PriceCode;
			clientName = priceInfo.FirmShortName;
			priceName = priceInfo.PriceName;
		}

		public FormalizeException(string message, Int64 ClientCode,
			Int64 PriceCode, string ClientName, string PriceName)
			: base(message)
		{
			clientCode = ClientCode;
			priceCode = PriceCode;
			clientName = ClientName;
			priceName = PriceName;
		}

		public string FullName
		{
			get { return String.Format("{0} ({1})", clientName, priceName); }
		}
	}

	public class RollbackFormalizeException : WarningFormalizeException
	{
		public readonly int FormCount = -1;
		//Кол-во "нулей"
		public readonly int ZeroCount = -1;
		//Кол-во нераспознанных событий
		public readonly int UnformCount = -1;
		//Кол-во "запрещенных" позиций
		public readonly int ForbCount = -1;

		public RollbackFormalizeException(string message, PriceFormalizationInfo priceInfo, FormLog stat) : base(message, priceInfo)
		{
			FormCount = (int)stat.Form.GetValueOrDefault();
			ZeroCount = (int)stat.Zero.GetValueOrDefault();
			UnformCount = (int)stat.UnForm.GetValueOrDefault();
			ForbCount = (int)stat.Forb.GetValueOrDefault();
		}
	}
}