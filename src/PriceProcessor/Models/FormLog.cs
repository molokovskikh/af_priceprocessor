using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("FormLogs", Schema = "Logs")]
	public class FormLog : ActiveRecordLinqBase<FormLog>
	{
		public int InsertProducerSynonymCount;
		public int InsertProductSynonymCount;

		public FormLog()
		{
		}

		public FormLog(PriceFormalizationInfo priceInfo)
		{
			Host = Environment.MachineName;
			PriceItemId = (uint?)priceInfo.PriceItemId;
			InsertCoreCount = 0;
			UpdateCoreCount = 0;
			DeleteCoreCount = 0;

			InsertCostCount = 0;
			UpdateCostCount = 0;
			DeleteCostCount = 0;

			Form = 0;
			Zero = 0;
			UnForm = 0;
			Forb = 0;
		}

		[PrimaryKey]
		public virtual uint RowId { get; set; }

		[Property]
		public virtual DateTime? LogTime { get; set; }

		[Property]
		public virtual uint? PriceItemId { get; set; }

		[Property]
		public virtual string Host { get; set; }

		[Property]
		public virtual int? ResultId { get; set; }

		[Property]
		public virtual uint? Form { get; set; }

		[Property]
		public virtual uint? UnForm { get; set; }

		[Property]
		public virtual uint? Zero { get; set; }

		[Property]
		public virtual uint? Forb { get; set; }

		[Property]
		public virtual uint? TotalSecs { get; set; }

		[Property]
		public virtual string Addition { get; set; }

		[Property]
		public virtual uint? DownloadId { get; set; }

		[Property]
		public virtual int InsertCoreCount { get; set; }

		[Property]
		public virtual int UpdateCoreCount { get; set; }

		[Property]
		public virtual int DeleteCoreCount { get; set; }

		[Property]
		public virtual int InsertCostCount { get; set; }

		[Property]
		public virtual int UpdateCostCount { get; set; }

		[Property]
		public virtual int DeleteCostCount { get; set; }

		public virtual int MaxLockCount { get; set; }

		public void Fix(string downloadId, int minRepeatTranCount)
		{
			LogTime = DateTime.Now;
			DownloadId = NullableConvert.ToUInt32(downloadId);
			ResultId = (int)((MaxLockCount <= Settings.Default.MinRepeatTranCount) ? FormResults.OK : FormResults.Warrning);
		}

		public override string ToString()
		{
			return $"{base.ToString()}," +
				$"{nameof(Form)}: {Form}, {nameof(UnForm)}: {UnForm}, {nameof(Zero)}: {Zero}, {nameof(Forb)}: {Forb}, {nameof(TotalSecs)}: {TotalSecs}, " +
				$"{nameof(InsertCoreCount)}: {InsertCoreCount}, {nameof(UpdateCoreCount)}: {UpdateCoreCount}, {nameof(DeleteCoreCount)}: {DeleteCoreCount}, " +
				$"{nameof(InsertCostCount)}: {InsertCostCount}, {nameof(UpdateCostCount)}: {UpdateCostCount}, {nameof(DeleteCostCount)}: {DeleteCostCount}";
		}
	}
}