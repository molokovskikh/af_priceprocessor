﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("SupplierCodes", Schema = "Catalogs")]
	public class SupplierCode
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("PriceId")]
		public Price Price { get; set; }

		[BelongsTo("AssortmentId")]
		public Assortment Assortment { get; set; }

		[BelongsTo("ProductId")]
		public Product Product { get; set; }

		[Property]
		public int? ProducerId { get; set; }

		[Property]
		public string Code { get; set; }

		[Property]
		public string CodeCr { get; set; }
	}
}
