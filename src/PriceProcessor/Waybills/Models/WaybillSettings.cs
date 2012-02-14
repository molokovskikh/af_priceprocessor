﻿using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("RetClientsSet", Schema = "Usersettings")]
	public class WaybillSettings : ActiveRecordLinqBase<WaybillSettings>
	{
		[PrimaryKey("ClientCode")]
		public uint Id { get; set; }

		[Property]
		public bool IsConvertFormat { get; set; }

		[Property]
		public uint? AssortimentPriceId { get; set; }

		[Property]
		public bool ParseWaybills { get; set; }

		[Property]
		public bool OnlyParseWaybills { get; set; }

		[Property]
		public ProtekWaybillSavingType ProtekWaybillSavingType { get; set; }

		public bool ShouldParseWaybill()
		{
			return ParseWaybills || OnlyParseWaybills;
		}
	}
}