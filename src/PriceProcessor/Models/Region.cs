using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Regions", Schema = "farm")]
	public class Region : ActiveRecordLinqBase<Region>
	{
		[PrimaryKey("RegionCode")]
		public virtual ulong Id { get; set; }

		[Property("Region")]
		public virtual string Name { get; set; }

		[Property]
		public virtual string ShortAliase { get; set; }
		
	}
}