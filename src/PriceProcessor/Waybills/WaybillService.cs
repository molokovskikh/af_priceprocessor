using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	[ServiceContract]
	public interface IWaybillService
	{
		[OperationContract]
		uint[] ParseWaybill(uint[] uints);
	}

	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
	public class WaybillService : IWaybillService
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (WaybillService));

		public List<EMailSourceHandlerException> Exceptions = new List<EMailSourceHandlerException>();

		public uint[] ParseWaybill(uint[] ids)
		{
			try {
				using (var scope = new TransactionScope(OnDispose.Rollback)) {
					var result = ParseWaybills(DocumentReceiveLog.LoadByIds(ids), false)
						.Select(d => d.Id)
						.ToArray();

					scope.VoteCommit();
					return result;
				}
			}
			catch (Exception e) {
				_log.Error("Ошибка при разборе накладных", e);
			}
			return new uint[0];
		}

		public static void ParseWaybill(DocumentReceiveLog log)
		{
			ParseWaybills(new [] {log}.ToList());
		}
		public void Process(List<DocumentReceiveLog> logs)
		{
			try {
				using (var scope = new TransactionScope(OnDispose.Rollback)) {
					ParseWaybills(logs, true);

					scope.VoteCommit();
				}
			}
			catch(Exception e) {
				_log.Error("Ошибка при разборе накладных", e);
			}
		}

		public static void ParseWaybills(List<DocumentReceiveLog> logs)
		{
			new WaybillService().Process(logs);
		}

		private IEnumerable<Document> ParseWaybills(List<DocumentReceiveLog> logs, bool shouldCheckClientSettings)
		{
			if (shouldCheckClientSettings)
				logs = CheckDocs(logs);

			var docsForParsing = MultifileDocument.Merge(logs);
			var docs = docsForParsing.Select(d => {
				try {
					return Process(shouldCheckClientSettings, d.DocumentLog, d.FileName);
				}
				catch (Exception e) {
					var filename = d.FileName;
					_log.Error(String.Format("Не удалось разобрать накладную {0}", filename), e);
					SaveWaybill(filename);
					return null;
				}
			}).Where(d => d != null).ToList();
			MultifileDocument.DeleteMergedFiles(docsForParsing);

			docs.Each(d => {
				if(d.Log.IsFake) d.Log.Save();
				d.Save();
				d.CreateCertificateTasks();
			});
			return docs;
		}

		private List<DocumentReceiveLog> CheckDocs(List<DocumentReceiveLog> logs)
		{
			return logs.Select(l => {
				try {
					SessionHelper.WithSession(s => l.Check(s));
					l.SaveAndFlush();
					var settings = WaybillSettings.Find(l.ClientCode.Value);

					if (!(l.DocumentType == DocType.Waybill && settings.IsConvertFormat))
						l.CopyDocumentToClientDirectory();
					return l;
				}
				catch(EMailSourceHandlerException e) {
					_log.Warn(String.Format("Не удалось разобрать накладную {0}", l.FileName), e);
					Exceptions.Add(e);
					return null;
				}
			}).Where(l => l != null).ToList();
		}

		private static Document Process(bool shouldCheckClientSettings,
			DocumentReceiveLog log,
			string fileName)
		{
			var settings = WaybillSettings.Find(log.ClientCode.Value);
			if (log.DocumentType == DocType.Reject)
				return null;

			if (shouldCheckClientSettings
				&& !settings.ShouldParseWaybill())
				return null;

			// ждем пока файл появится в удаленной директории
			if (!log.FileIsLocal())
				ShareFileHelper.WaitFile(fileName, 5000);

			var doc = new WaybillFormatDetector().DetectAndParse(log, fileName);
			// для мульти файла, мы сохраняем в источнике все файлы,
			// а здесь, если нужна накладная в dbf формате, то сохраняем merge-файл в dbf формате.
			if (doc != null)
				Exporter.ConvertIfNeeded(doc, settings);

			return doc;
		}

		public static void SaveWaybill(string filename)
		{
			if (!Directory.Exists(Settings.Default.DownWaybillsPath))
				Directory.CreateDirectory(Settings.Default.DownWaybillsPath);

			if (File.Exists(filename))
				File.Copy(filename, Path.Combine(Settings.Default.DownWaybillsPath, Path.GetFileName(filename)), true);
		}
	}
}
