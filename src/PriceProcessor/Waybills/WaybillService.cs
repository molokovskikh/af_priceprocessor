using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using log4net;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

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

		public uint[] ParseWaybill(uint[] ids)
		{
			try
			{
				using (new SessionScope())
				{
					return ParseWaybills(DocumentReceiveLog.LoadByIds(ids), false)
						.Select(d => d.Id)
						.ToArray();
				}
			}
			catch (Exception e)
			{
				_log.Error("Ошибка при разборе накладных", e);
			}
			return new uint[0];
		}

		public static void ParseWaybills(List<DocumentReceiveLog> logs)
		{
			try
			{
				ParseWaybills(logs, true);
			}
			catch(Exception e)
			{
				_log.Error("Ошибка при разборе накладных", e);
			}
		}

		private static IEnumerable<Document> ParseWaybills(List<DocumentReceiveLog> logs, bool shouldCheckClientSettings)
		{
			var detector = new WaybillFormatDetector();
			
			var docsForParsing = MultifileDocument.Merge(logs);

			var docs = docsForParsing.Select(d => {
				
				try
				{
					var settings = WaybillSettings.Find(d.DocumentLog.ClientCode.Value);
					
					if (d.DocumentLog.DocumentType == DocType.Reject)
						return null;
					if (shouldCheckClientSettings && settings != null && !settings.ShouldParseWaybill())
						return null;
					
					if(!d.DocumentLog.FileIsLocal())
					{
						// ждем пока файл появится в удаленной директории
						ShareFileHelper.WaitFile(d.FileName, 5000);
					}

					var doc = detector.DetectAndParse(d.DocumentLog, d.FileName);
					
					// для мульти файла, мы сохраняем в источнике все файлы, 
					// а здесь, если нужна накладная в dbf формате, то сохраняем merge-файл в dbf формате.
					if (doc != null && settings != null) {
						using (var scope = new TransactionScope(OnDispose.Rollback)) {
							Exporter.ConvertIfNeeded(doc, settings);
							scope.VoteCommit();
						}
					}

					return doc;
				}
				catch (Exception e)
				{
					var filename = d.FileName;
					_log.Error(String.Format("Не удалось разобрать накладную {0}", filename), e);
					SaveWaybill(filename);
					return null;
				}
			}).Where(d => d != null).ToList();
			MultifileDocument.DeleteMergedFiles(docsForParsing);

			using (var scope = new TransactionScope(OnDispose.Rollback))
			{

				docs.Each(d => {
					if(d.Log.IsFake) d.Log.Save();
					d.Save();
					d.CreateCertificateTasks();
				});
				scope.VoteCommit();
			}
			return docs;
		}

		public static void SaveWaybill(string filename)
		{			
			if (!Directory.Exists(Settings.Default.DownWaybillsPath))
				Directory.CreateDirectory(Settings.Default.DownWaybillsPath);

			if (File.Exists(filename))
				File.Copy(filename, Path.Combine(Settings.Default.DownWaybillsPath, Path.GetFileName(filename)), true);
		}

		public static void ParserDocument(DocumentReceiveLog log)
		{
			var file = log.GetFileName();

			try
			{
				var settings = WaybillSettings.Find(log.ClientCode.Value);
					
				if (!settings.IsConvertFormat)
					log.CopyDocumentToClientDirectory();

				if (!settings.ShouldParseWaybill() || (log.DocumentType == DocType.Reject))
					return;
					
				var document = new WaybillFormatDetector().DetectAndParse(log, file);
				if (document == null)
					return;

				using (var transaction = new TransactionScope(OnDispose.Rollback))
				{
					Exporter.ConvertIfNeeded(document, settings);

					if(log.IsFake) log.Save();
					document.Save();
					document.CreateCertificateTasks();

					transaction.VoteCommit();
				}
			}
			catch(Exception e)
			{
				_log.Error(String.Format("Ошибка при разборе документа {0}", file), e);
				SaveWaybill(file);
			}
		}
	}
}
