using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Suppliers", Schema = "Future")]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public string Name { get; set; }
		
		[Property]
		public string FullName { get; set; }
	}
}