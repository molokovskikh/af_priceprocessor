using System;
using System.Data;
using Common.Tools;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class FormalizationPosition
	{
		private DataRow productSynonym;
		private DataRow producerSynonym;

		public NewOffer Offer;
		public UnrecExpStatus Status { get; set; }

		public string PositionName { get; set; }
		public string OriginalName { get; set; }
		public string Code { get; set; }
		public string FirmCr { get; set; }

		public long? ProductId { get; set; }
		public long? CatalogId { get; set; }
		public bool? Pharmacie { get; set; }

		public long? SynonymCode
		{
			get
			{
				if (productSynonym == null)
					return null;
				if (productSynonym["SynonymCode"] is DBNull)
					return null;
				return Convert.ToInt64(productSynonym["SynonymCode"]);
			}
		}

		public long? CodeFirmCr { get; set; }
		public long? SynonymFirmCrCode
		{
			get
			{
				if (producerSynonym == null)
					return null;
				if (producerSynonym["SynonymFirmCrCode"] is DBNull)
					return null;
				return Convert.ToInt64(producerSynonym["SynonymFirmCrCode"]);
			}
		}

		public long? InternalProducerSynonymId
		{
			get
			{
				if (producerSynonym == null)
					return null;
				if (producerSynonym["InternalProducerSynonymId"] is DBNull)
					return null;
				return Convert.ToInt64(producerSynonym["InternalProducerSynonymId"]);
			}
		}

		public bool IsAutomaticProducerSynonym { get; set; }

		public bool NotCreateUnrecExp { get; set; }

		public void UpdateProductSynonym(DataRow row)
		{
			productSynonym = row;
			ProductId = Convert.ToInt64(row["ProductId"]);
			CatalogId = Convert.ToInt64(row["CatalogId"]);
			Pharmacie = Convert.ToBoolean(row["Pharmacie"]);
			var isJunk = Convert.ToBoolean(row["Junk"]);
			if (isJunk)
				Offer.Junk = true;
			if (SynonymCode == null)
				Offer.CreatedProductSynonym = productSynonym;
			AddStatus(UnrecExpStatus.NameForm);
		}

		public void UpdateProducerSynonym(DataRow row)
		{
			producerSynonym = row;
			if (!Convert.IsDBNull(row["CodeFirmCr"]))
				CodeFirmCr = Convert.ToInt64(row["CodeFirmCr"]);
			IsAutomaticProducerSynonym = Convert.ToBoolean(row["IsAutomatic"]);
			if (SynonymFirmCrCode == null)
				Offer.CreatedProducerSynonym = row;
			if (!IsAutomaticProducerSynonym && !NotCreateUnrecExp)
				AddStatus(UnrecExpStatus.FirmForm);
		}

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
			if (DateTime.TryParse(Offer.Period, out periodAsDateTime)) {
				Offer.Exp = periodAsDateTime;
				var isJunk = SystemTime.Now() >= periodAsDateTime
					|| periodAsDateTime.Subtract(SystemTime.Now()).TotalDays < 180;
				if (isJunk)
					Offer.Junk = isJunk;
			}
		}
	}
}