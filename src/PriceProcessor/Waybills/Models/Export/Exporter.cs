using System;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Helpers;
using NHibernate.Linq;

namespace Inforoom.PriceProcessor.Waybills.Models.Export
{
	public enum WaybillFormat
	{
		Sst = 0,
		ProtekDbf = 1,
		SstLong = 2,
		LessUniversalDbf = 3,
		UniversalDbf = 4,
		InfoDrugstoreXml = 5,
		LipetskFarmacia = 6,
		InproXml = 7,
	}

	public class Exporter
	{
		public static DocumentReceiveLog Convert(Document document, WaybillFormat type, WaybillSettings settings)
		{
			var extention = settings.GetExportExtension(type);
			var log = document.Log;
			//если нет файла значит документ из сервиса протека
			if (String.IsNullOrEmpty(document.Log.FileName)) {
				log.IsFake = false;

				var id = (document.ProviderDocumentId ?? document.Log.Id.ToString()).Replace('/', '_');
				log.FileName = id + extention;
			}
			else {
				log.IsFake = true;
				log = new DocumentReceiveLog(log, extention);
				ActiveRecordMediator.SaveAndFlush(log);
			}

			var filename = log.GetRemoteFileNameExt();

			if (type == WaybillFormat.ProtekDbf) {
				DbfExporter.SaveProtek(document, filename);
			}
			else if (type == WaybillFormat.LessUniversalDbf) {
				DbfExporter.SaveUniversalV1(document, filename);
			}
			else if (type == WaybillFormat.UniversalDbf) {
				DbfExporter.SaveUniversalV2(document, filename);
			}
			else if (type == WaybillFormat.LipetskFarmacia) {
				document.Log.IsFake = false;
				ExcelExporter.SaveLipetskFarmacia(document, filename);
			}
			else if (type == WaybillFormat.InfoDrugstoreXml) {
				using(var session = SessionHelper.GetSessionFactory().OpenSession())
					XmlExporter.SaveInfoDrugstore(session, settings, document, filename);
			}
			else if (type == WaybillFormat.InproXml)
				using(var session = SessionHelper.GetSessionFactory().OpenSession()) {
					var map = session.Query<SupplierMap>().ToList();
					XmlExporter.SaveInpro(document, filename, map);
				}
			else {
				using (var fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1251))) {
						if (type == WaybillFormat.Sst)
							SstExport.SaveShort(document, sw);
						else
							SstExport.SaveLong(document, sw);
					}
				}
			}

			log.DocumentSize = new FileInfo(filename).Length;
			ActiveRecordMediator.Save(document.Log);
			ActiveRecordMediator.Save(log);
			return log;
		}

		public static void SaveProtek(Document document)
		{
			var settings = WaybillSettings.Find(document.ClientCode);
			var log = document.Log;
			var converted = ConvertIfNeeded(document, settings);
			var lipetskFarmaciaFlag = settings.IsConvertFormat && settings.WaybillConvertFormat == WaybillFormat.LipetskFarmacia;
			if (!converted || lipetskFarmaciaFlag) {
				Convert(document, settings.ProtekWaybillSavingType, settings);
				//липецкфармации мы даем 2 файла, но экспорт - делает фейковым изначальный лог файла и заменяет его, поэтому надо ручками сделать его не фейковым
				if (lipetskFarmaciaFlag)
					log.IsFake = false;
			}
		}

		public static bool ConvertIfNeeded(Document document, WaybillSettings settings)
		{
			if (settings.IsConvertFormat
				&& document.SetAssortimentInfo(settings)) {
				Convert(document, settings.WaybillConvertFormat, settings);
				return true;
			}
			return false;
		}
	}
}