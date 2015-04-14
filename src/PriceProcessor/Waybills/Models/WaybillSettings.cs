using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models.Export;

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
		public bool OnlyParseWaybills { get; set; }

		[Property]
		public WaybillFormat ProtekWaybillSavingType { get; set; }

		[Property]
		public WaybillFormat WaybillConvertFormat { get; set; }

		public string GetExportExtension(WaybillFormat type)
		{
			if (type == WaybillFormat.Sst
				|| type == WaybillFormat.SstLong)
				return ".sst";
			if (type == WaybillFormat.ProtekDbf
				|| type == WaybillFormat.LessUniversalDbf
				|| type == WaybillFormat.UniversalDbf)
				return ".dbf";
			if (type == WaybillFormat.InfoDrugstoreXml)
				return ".xml";
			if (type == WaybillFormat.LipetskFarmacia)
				return ".xls";
			return ".dat";
		}
	}
}