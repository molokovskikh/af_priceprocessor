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
						ConvertAndSaveDbfFormatIfNeeded(doc, d.DocumentLog, true);											

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
					ConvertAndSaveDbfFormatIfNeeded(document, log, true);						
										
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

		private static DataTable InitTableForFormatDbf(Document document, Supplier supplier)
		{
			var table = new DataTable();

			table.Columns.AddRange(new DataColumn[]
									{
										new DataColumn("postid_af"),
										new DataColumn("post_name_af"),
										new DataColumn("apt_af"),
										new DataColumn("aname_af"),
										new DataColumn("ttn"),
										new DataColumn("ttn_date"),
										new DataColumn("id_artis"),
										new DataColumn("name_artis"),
										new DataColumn("przv_artis"),
										new DataColumn("name_post"),
										new DataColumn("przv_post"),
										new DataColumn("seria"),
										new DataColumn("sgodn"),
										new DataColumn("sert"),
										new DataColumn("sert_date"),
										new DataColumn("prcena_bnds"),
										new DataColumn("gr_cena"),
										new DataColumn("pcena_bnds"),
										new DataColumn("nds"),
										new DataColumn("pcena_nds"),
										new DataColumn("kol_tov"),
										new DataColumn("ean13")
									});
			

			foreach (var line in document.Lines)
			{
				var row = table.NewRow();
				row["postid_af"] = document.FirmCode;
				row["post_name_af"] = supplier.FullName;
				row["apt_af"] = document.AddressId;
				row["aname_af"] = document.AddressId != null
									? With.Connection(connection =>
													  MySqlHelper.ExecuteScalar(connection,
																				"select Address from future.Addresses where Id = " +
																				document.AddressId
														))
									: "";
				row["ttn"] = document.ProviderDocumentId;
				row["ttn_date"] = document.DocumentDate;				
				if(line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.Code != null)
					row["id_artis"] = line.AssortimentPriceInfo.Code;				
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.Synonym != null)
					row["name_artis"] = line.AssortimentPriceInfo.Synonym;
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.SynonymFirmCr != null)
					row["przv_artis"] = line.AssortimentPriceInfo.SynonymFirmCr;
				row["name_post"] = line.Product;
				row["przv_post"] = line.Producer;
				row["seria"] = line.SerialNumber;
				row["sgodn"] = line.Period;
				row["sert"] = line.Certificates;				
				row["sert_date"] = line.CertificatesDate;
				row["prcena_bnds"] = line.ProducerCostWithoutNDS;
				row["gr_cena"] = line.RegistryCost;
				row["pcena_bnds"] = line.SupplierCostWithoutNDS;
				row["nds"] = line.Nds;
				row["pcena_nds"] = line.SupplierCost;
				row["kol_tov"] = line.Quantity;
				row["ean13"] = line.EAN13;
				
				table.Rows.Add(row);
			}

			return table;
		}
		
		public static void ConvertAndSaveDbfFormatIfNeeded(Document document, DocumentReceiveLog log, bool isfake = false)
		{
			if (document.SetAssortimentInfo() == false) return;
			var path = string.Empty;
			try
			{	
				var table = InitTableForFormatDbf(document, log.Supplier);

				using (var scope = new TransactionScope(OnDispose.Rollback))
				{
					var log_dbf = DocumentReceiveLog.Log(	log.Supplier.Id,
															log.ClientCode,
															log.AddressId,
															//Path.GetFileNameWithoutExtension(log.GetRemoteFileNameExt()) + ".dbf",
															Path.GetFileNameWithoutExtension(log.FileName) + ".dbf",
															log.DocumentType, 
															"Сконвертированный Dbf файл"
														);

					var file = log_dbf.GetRemoteFileNameExt();
					log_dbf.FileName = string.Format("{0}{1}", Path.GetFileNameWithoutExtension(file), ".dbf");
					log_dbf.SaveAndFlush();

					path = Path.Combine(Path.GetDirectoryName(file), log_dbf.FileName);
					//сохраняем накладную в новом формате dbf.
					Dbf.Save(table, path);

					scope.VoteCommit();
				}
				log.IsFake = isfake;
			}
			catch (Exception exception)
			{
				var info = string.Format("Исходный файл: {0} , log.id = {1}. Сконвертированный: {2}. ClientCode = {3}. SupplierId = {4}.", log.FileName, log.Id, path, log.ClientCode, log.Supplier.Id);
				_log.Error("Ошибка сохранения накладной в новый формат dbf. "+ info + " Ошибка: " + exception.Message + ". StackTrace:" + exception.StackTrace);
				throw;
			}
		}

		public static void ComparisonWithOrders(Document document, IList<OrderHead> orders)
		{
			if (document == null) return;
			if (document.OrderId == null) return;
			if (orders == null) return;
			if (orders.Count == 0) return;
			try
			{
				var waybillPositions = document.Lines.Where(l => l != null && !String.IsNullOrEmpty(l.Code)).ToList();

				while (waybillPositions.Count > 0)
				{
					var line = NextFirst(waybillPositions, false);                    
					var code = line.Code.Trim().ToLower();
					var waybillLines = waybillPositions.Where(l => l.Code.Trim().ToLower() == code).ToList();
					foreach (var waybillLine in waybillLines)
					{
						waybillPositions.Remove(waybillLine);
					}
					foreach (var itemW in waybillLines)
					{
						foreach (var order in orders)
						{
							var orderLines = order.Items.Where(i => i.Code.Trim().ToLower() == code).ToList();
							foreach (var itemOrd in orderLines)
							{
								AddToAssociativeTable(itemW.Id, itemOrd.Id);
							}
						}
					}
				}
			}
			catch(Exception e)
			{
				_log.Error("Ошибка при сопоставлении заказов накладной", e);
			}
		}

		private static T NextFirst<T> (IList<T> list, bool rem) where T : class
		{
			if (list.Count == 0) return null;                
			if(rem)
			{
				list.Remove(list.First());
			}
			if (list.Count == 0) return null;
			return list.First();
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
