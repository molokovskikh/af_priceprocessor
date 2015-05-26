using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Rejects
{
	public abstract class RejectParser
	{
		public RejectHeader CreateReject(DocumentReceiveLog log)
		{
			var rejectheader = new RejectHeader(log);
			var filename = log.GetFileName();
			Parse(rejectheader, filename);
			return rejectheader;
		}

		public abstract void Parse(RejectHeader rejectHeader, string filename);
	}
}
