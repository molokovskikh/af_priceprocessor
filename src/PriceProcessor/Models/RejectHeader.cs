using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Type;

namespace Inforoom.PriceProcessor.Models
{
	/// <summary>
	/// Заголовок документа отказа. Содержит строки отказа.
	/// В целом, представляет собой не только заголовок, но и сам документ отказа.
	/// </summary>
	[ActiveRecord(Schema = "Documents")]
	public class RejectHeader
	{
		public RejectHeader()
		{
			Lines = new List<RejectLine>();
		}

		public RejectHeader(DocumentReceiveLog log)
			: this()
		{
			Log = log;
			WriteTime = DateTime.Now;
			Address = log.Address;
			Supplier = log.Supplier;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime WriteTime { get; set; }

		[Property]
		public virtual string Parser { get; set; }

		[BelongsTo("SupplierId")]
		public virtual Supplier Supplier { get; set; }

		[BelongsTo("AddressId")]
		public virtual Address Address { get; set; }

		[BelongsTo("DownloadId")]
		public virtual DocumentReceiveLog Log { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<RejectLine> Lines { get; set; }

		/// <summary>
		/// Функция, которая пытается связать текстовые данные в строках отказа в реальные связи с моделями.
		/// </summary>
		/// <param name="session">Сессия БД</param>
		public void Normalize(ISession session)
		{
			var priceIds = session.Query<Price>().Where(p => p.Supplier == Supplier)
				.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToArray();
			if (priceIds.Length <= 0)
				return;
			var productNames = Lines.Select(l => ProductSynonym.MakeCanonical(l.Product)).ToArray();
			var productSynonyms = session.Query<ProductSynonym>()
				.Where(s => productNames.Contains(s.Canonical) && s.Product != null && priceIds.Contains(s.Price.Id))
				.ToArray();
			var productLookup = productSynonyms.ToLookup(s => s.Canonical, s => s.Product, StringComparer.CurrentCultureIgnoreCase);

			var producerNames = Lines.Select(l => ProductSynonym.MakeCanonical(l.Producer)).ToArray();
			var producerSynonyms = session.Query<ProducerSynonym>()
				.Where(s => producerNames.Contains(s.Canonical) && s.Producer != null && priceIds.Contains(s.Price.Id))
				.ToArray();
			var producerLookup = producerSynonyms.ToLookup(s => s.Canonical, s => s.Producer, StringComparer.CurrentCultureIgnoreCase);

			foreach (var line in Lines) {
				line.ProductEntity = productLookup[ProductSynonym.MakeCanonical(line.Product)].FirstOrDefault();
				line.ProducerEntity = producerLookup[ProductSynonym.MakeCanonical(line.Producer)].FirstOrDefault();
			}
		}
	}

	/// <summary>
	/// Строка документа отказа.
	/// </summary>
	[ActiveRecord(Schema = "Documents")]
	public class RejectLine
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		//Обязательное поле
		[Property, Description("Код товара")]
		public virtual string Code { get; set; }

		[Property, Description("Наименование товара")]
		public virtual string Product { get; set; }

		[Property, Description("Производитель товара")]
		public virtual string Producer { get; set; }

		[Property, Description("Количество заказанных товаров")]
		public virtual uint? Ordered { get; set; }

		//Обязательное поле
		[Property, Description("Количество отказов по товару")]
		public virtual uint Rejected { get; set; }

		[Property, Description("Стоимость товара")]
		public virtual decimal? Cost { get; set; }

		[Property, Description("Код производителя, строка макс 255 символов")]
		public virtual string CodeCr { get; set; }
		
		[Property, Description("Номер заявки АналитФАРМАЦИЯ")]
		public virtual uint OrderId { get; set; }

		[BelongsTo("HeaderId")]
		public virtual RejectHeader Header { get; set; }

		[BelongsTo("ProductId")]
		public virtual Product ProductEntity { get; set; }

		[BelongsTo("ProducerId")]
		public virtual Producer ProducerEntity { get; set; }
	}
}