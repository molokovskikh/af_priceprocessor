using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Catalog", Schema = "Catalogs")]
	public class Catalog : ActiveRecordLinqBase<Catalog>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Pharmacie { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public bool Hidden { get; set; }

		[Property]
		public bool Monobrend { get; set; }

		[Property]
		public uint NameId { get; set; }

		[Property]
		public uint FormId { get; set; }
	}
}