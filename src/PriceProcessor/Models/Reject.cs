using System;
using System.Data;
using Castle.ActiveRecord;
using Inforoom.Formalizer;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord(Schema = "Farm", Table = "Rejects")]
	public class Reject
	{
		private bool marked;

		public Reject()
		{
		}

		public Reject(FormalizationPosition position, DataRow row)
		{
			Product = position.PositionName;
			ProductId = (uint?)position.ProductId;
			Producer = position.FirmCr;
			ProducerId = (uint?)position.CodeFirmCr;
			Series = row["SERNM"].ToString();
			LetterNo = row["LETTERSNR"].ToString();
			LetterDate = (DateTime)row["LETTERSDT"];
			CauseRejects = row["QUALNMR"].ToString();
		}

		[PrimaryKey]
		public uint Id { set; get; }

		[Property]
		public string Product { get; set; }

		[Property]
		public uint? ProductId { get; set; }

		[Property]
		public string Producer { get; set; }

		[Property]
		public uint? ProducerId { get; set; }

		[Property]
		public string Series { get; set; }

		[Property]
		public string LetterNo { get; set; }

		[Property]
		public DateTime LetterDate { get; set; }

		[Property]
		public string CauseRejects { get; set; }

		[Property]
		public DateTime? CancelDate { get; set; }

		public bool Equivalent(Reject reject)
		{
			if (marked)
				return false;
			var result = Product == reject.Product
				&& ProductId == reject.ProductId
				&& Producer == reject.Producer
				&& ProducerId == reject.ProducerId
				&& Series == reject.Series
				&& LetterNo == reject.LetterNo
				&& LetterDate == reject.LetterDate
				&& CauseRejects == reject.CauseRejects;
			if (result)
				marked = true;
			return result;
		}

		public bool CheckCancellation(Reject saved)
		{
			if (ProductId == null || saved.ProductId == null)
				return false;

			var canceled = ProductId == saved.ProductId
				&& Series == saved.Series
				&& saved.LetterDate <= LetterDate;
			if (canceled)
				saved.CancelDate = LetterDate;
			return canceled;
		}
	}
}