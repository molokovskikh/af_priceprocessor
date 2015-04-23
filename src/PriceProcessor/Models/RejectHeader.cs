using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using NHibernate.Type;

namespace Inforoom.PriceProcessor.Models
{
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

		[BelongsTo("SupplierId")]
		public virtual Supplier Supplier { get; set; }

		[BelongsTo("AddressId")]
		public virtual Address Address { get; set; }

		[BelongsTo("DownloadId")]
		public virtual DocumentReceiveLog Log { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<RejectLine> Lines { get; set; }

		public static RejectHeader ReadReject(DocumentReceiveLog log, string fileName)
		{
			var reject = new RejectHeader(log);
			using (var reader = new StreamReader(File.OpenRead(fileName), Encoding.GetEncoding(1251))) {
				string line;
				var rejectFound = false;
				while ((line = reader.ReadLine()) != null) {
					if (line.Trim() == "О Т К А З Ы") {
						rejectFound = true;
						continue;
					}
					if (line.Trim() == "СФОРМИРОВАННЫЙ ЗАКАЗ") {
						break;
					}
					if (!rejectFound)
						continue;
					if (line.Length == 0)
						continue;
					//пропускаем заголовок
					if (line[0] == '¦')
						continue;
					//пропускаем разделители строк
					if (line.All(c => c == '-'))
						continue;
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Product = line.Substring(0, 35).Trim();
					rejectLine.Producer = line.Substring(35, 13).Trim();
					rejectLine.Cost = NullableConvert.ToDecimal(line.Substring(48, 9).Trim(), CultureInfo.InvariantCulture);
					rejectLine.Ordered = (uint?)NullableConvert.ToFloatInvariant(line.Substring(57, 9).Trim());
					var rejectedCount = (rejectLine.Ordered - (uint?)NullableConvert.ToFloatInvariant(line.Substring(66, 9).Trim()));
					rejectLine.Rejected = rejectedCount.GetValueOrDefault();
				}
			}
			return reject;
		}
	}

	[ActiveRecord(Schema = "Documents")]
	public class RejectLine
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("HeaderId")]
		public virtual RejectHeader Header { get; set; }

		[Property]
		public virtual string Product { get; set; }

		[Property]
		public virtual string Producer { get; set; }

		[Property]
		public virtual decimal? Cost { get; set; }

		[Property]
		public virtual uint? Ordered { get; set; }

		[Property]
		public virtual uint Rejected { get; set; }
	}
}