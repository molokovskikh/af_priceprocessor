using System;
using Common.Tools;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class FormalizationPosition
	{
		public NewCore Core;
		public UnrecExpStatus Status { get; set; }

		public string PositionName { get; set; }
		public string OriginalName { get; set; }
		public string Code { get; set; }
		public string FirmCr { get; set; }

		public bool Junk { get; set; }

		public long? ProductId { get; set; }
		public long? CatalogId { get; set; }
		public bool? Pharmacie { get; set; }
		public long? SynonymCode { get; set; }
		public long? CodeFirmCr { get; set; }
		public long? SynonymFirmCrCode { get; set; }

		public bool IsAutomaticProducerSynonym { get; set; }
		public long? InternalProducerSynonymId { get; set; }

		public bool NotCreateUnrecExp { get; set; }

		public void AddStatus(UnrecExpStatus status)
		{
			Status |= status;
		}

		public bool IsSet(UnrecExpStatus checkStatus)
		{
			return ((Status & checkStatus) == checkStatus);
		}

		public bool IsNotSet(UnrecExpStatus checkStatus)
		{
			return ((Status & checkStatus) != checkStatus);
		}

		public void CalculateJunk()
		{
			DateTime periodAsDateTime;
			if (DateTime.TryParse(Core.Period, out periodAsDateTime)) {
				Core.Exp = periodAsDateTime;
				var isJunk = SystemTime.Now() >= periodAsDateTime
					|| periodAsDateTime.Subtract(SystemTime.Now()).TotalDays < 180;
				if (isJunk)
					Core.Junk = isJunk;
			}
		}
	}
}