using System.ComponentModel;

namespace Inforoom.PriceProcessor.Models
{
	public enum DocType
	{
		[Description("Накладная")] Waybill = 1,
		[Description("Отказ")] Reject = 2
	}
}