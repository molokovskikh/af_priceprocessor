using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Suppliers", Schema = "Customers")]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
		public Supplier()
		{
			ExcludeFiles = new List<WaybillExcludeFile>();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string FullName { get; set; }

		[Property]
		public virtual ulong RegionMask { get; set; }

		[Property]
		public virtual uint? Payer { get; set; }

		[Property]
		public virtual string RejectParser { get; set; }

		[HasMany(Lazy = true, Cascade = ManyRelationCascadeEnum.All)]
		public virtual IList<WaybillExcludeFile> ExcludeFiles { get; set; }

		public override string ToString()
		{
			return $"{Name} ({Id})";
		}
	}
}