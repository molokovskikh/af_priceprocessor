using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using MySql.Data.MySqlClient;
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
					if (doc != null && settings != null && settings.IsConvertFormat)
					{
						using (var scope = new TransactionScope(OnDispose.Rollback))
						{
							DbfExporter.ConvertAndSaveDbfFormatIfNeeded(doc);
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

				//конвертируем накладную в новый формат dbf.
				if (settings.IsConvertFormat)
				{
					using (var scope = new TransactionScope(OnDispose.Rollback))
					{
						DbfExporter.ConvertAndSaveDbfFormatIfNeeded(document);
						scope.VoteCommit();
					}
				}

				using (var transaction = new TransactionScope(OnDispose.Rollback))
				{
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


	public class WaybillOrderMatcher
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(WaybillOrderMatcher));

		public static void ComparisonWithOrders(Document document, IList<OrderHead> orders)
		{
			if (document == null) return;			
			using (new SessionScope())
			{
				try
				{					
					if (orders != null) // заказы переданы отдельно и не связаны с позициями в накладной
					{
						var waybillPositions = document.Lines.Where(l => l != null && !String.IsNullOrEmpty(l.Code)).ToList();

						while (waybillPositions.Count > 0)
						{
							var line = waybillPositions.First();
							var code = line.Code.Trim().ToLower();
							var waybillLines = waybillPositions.Where(l => l.Code.Trim().ToLower() == code).ToList();
							waybillLines.ForEach(waybillLine => waybillPositions.Remove(waybillLine));							
							foreach (var itemW in waybillLines)
							{
								foreach (var order in orders)
								{
									var orderLines = order.Items.Where(i => i != null && !String.IsNullOrEmpty(i.Code) && i.Code.Trim().ToLower() == code).ToList();
									orderLines.ForEach(itemOrd => AddToAssociativeTable(itemW.Id, itemOrd.Id));									
								}
							}
						}
					}
					else
					{
						var waybillPositions = document.Lines.Where(l => l != null && l.OrderId != null && !String.IsNullOrEmpty(l.Code)).ToList();						
						foreach(var line in waybillPositions) // номер заказа выставлен для каждой позиции в накладной
						{
							var code = line.Code.Trim().ToLower();
							var order = OrderHead.TryFind(line.OrderId);
							if (order == null) continue;
							var orderLines = order.Items.Where(i => i.Code.Trim().ToLower() == code).ToList();
							orderLines.ForEach(itemOrd => AddToAssociativeTable(line.Id, itemOrd.Id));
						}
					}
				}
				catch (Exception e)
				{
					_log.Error(String.Format("Ошибка при сопоставлении заказов накладной {0}", document.Id), e);
				}
			}
		}

		private static void AddToAssociativeTable(uint docLineId, uint ordLineId)
		{
			With.Connection(c =>
			{
				var command = new MySqlCommand(@"
insert into documents.waybillorders(DocumentLineId, OrderLineId)
values(?DocumentLineId, ?OrderLineId);
", c);
				command.Parameters.AddWithValue("?DocumentLineId", docLineId);
				command.Parameters.AddWithValue("?OrderLineId", ordLineId);
				command.ExecuteNonQuery();
			});
		}
	}
}
