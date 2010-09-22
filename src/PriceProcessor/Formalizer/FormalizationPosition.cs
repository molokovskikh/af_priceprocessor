using Inforoom.PriceProcessor.Formalizer.New;

namespace Inforoom.Formalizer
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
	}
}
