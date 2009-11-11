using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inforoom.Formalizer
{
	public class FormalizationPosition
	{
		public UnrecExpStatus Status { get; set; }

		public string PositionName { get; set; }
		public string OriginalName { get; set; }
		public string Code { get; set; }
		public string FirmCr { get; set; }

		public bool Junk { get; set; }

		public long? ProductId { get; set; }
		public long? CatalogId { get; set; }
		public long? SynonymCode { get; set; }
		public long? CodeFirmCr { get; set; }
		public long? SynonymFirmCrCode { get; set; }

		public bool IsAutomaticProducerSynonym { get; set; }
		public long? InternalProducerSynonymId { get; set; }

		public void AddStatus(UnrecExpStatus status)
		{
			Status |= status;
			//Если CodeFirmCr не выставлено, но позиция распознана по производителю и наименованию, 
			//то считаем, что формализовано по ассортименту
			if (!CodeFirmCr.HasValue && IsSet(UnrecExpStatus.NameForm) && IsSet(UnrecExpStatus.FirmForm))
				Status |= UnrecExpStatus.AssortmentForm;
		}

		public bool IsSet(UnrecExpStatus checkStatus)
		{
			return ((Status & checkStatus) == checkStatus);
		}

		public bool IsNotSet(UnrecExpStatus checkStatus)
		{
			return ((Status & checkStatus) != checkStatus);
		}

		public bool IsHealth()
		{
			return (IsNotSet(UnrecExpStatus.FirmForm) ||
				IsSet(UnrecExpStatus.AssortmentForm) ||
				IsSet(UnrecExpStatus.MarkExclude));
		}
	}
}
