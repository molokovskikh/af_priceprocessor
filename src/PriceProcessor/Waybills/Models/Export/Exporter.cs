using System;
using System.IO;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Helpers;

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
	}

	public class Exporter
	{
		/// <summary>
		/// Сохраняет данные в файл.
		/// </summary>
		public static void Save(Document document, WaybillFormat type, WaybillSettings settings)
		{
			var log = Convert(document, type, settings);

			ActiveRecordMediator.Save(document.Log);
			ActiveRecordMediator.Save(log);
		}

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
				DbfExporter.SaveLessUniversal(document, filename);
			}
			else if (type == WaybillFormat.UniversalDbf) {
				DbfExporter.SaveUniversalDbf(document, filename);
			}
			else if (type == WaybillFormat.InfoDrugstoreXml) {
				using(var session = SessionHelper.GetSessionFactory().OpenSession())
					XmlExporter.SaveInfoDrugstore(session, settings, document, filename);
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
			return log;
		}

		public static void SaveProtek(Document document)
		{
			var settings = WaybillSettings.Find(document.ClientCode);
			if (!ConvertIfNeeded(document, settings)) {
				Save(document, settings.ProtekWaybillSavingType, settings);
			}
		}

		public static bool ConvertIfNeeded(Document document, WaybillSettings settings)
		{
			if (settings.IsConvertFormat
				&& document.SetAssortimentInfo(settings)) {
				Save(document, settings.WaybillConvertFormat, settings);
				return true;
			}
			return false;
		}
	}
}