using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("OrdersHead", Schema = "Orders")]
	public class OrderHead : ActiveRecordLinqBase<OrderHead>
	{
		public OrderHead()
		{
			Items = new List<OrderItem>();
		}

		[PrimaryKey("RowId")]
		public virtual uint Id { get; set; }

		[Property]
		public DateTime WriteTime { get; set; }

		[BelongsTo("AddressId")]
		public virtual Address Address { get; set; }

		[Property]
		public virtual uint ClientCode { get; set; }

		[BelongsTo("PriceCode")]
		public virtual Price Price { get; set; }

		[HasMany(ColumnKey = "OrderId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<OrderItem> Items { get; set; }
	}
}