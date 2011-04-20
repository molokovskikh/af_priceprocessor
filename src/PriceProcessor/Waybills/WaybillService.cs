using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Properties;
using log4net;
using NHibernate.Criterion;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.PriceProcessor.Waybills
{
	[ServiceContract]
	public interface IWaybillService
	{
		[OperationContract]
		uint[] ParseWaybill(uint[] uints);
	}

	[ActiveRecord("Clientsdata", Schema = "Usersettings")]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
		[PrimaryKey("FirmCode")]
		public uint Id { get; set; }

		[Property]
		public string ShortName { get; set; }
		
		[Property]
		public string FullName { get; set; }
	}

	[ActiveRecord("RetClientsSet", Schema = "Usersettings")]
	public class WaybillSettings : ActiveRecordLinqBase<WaybillSettings>
	{
		[PrimaryKey("ClientCode")]
		public uint Id { get; set; }

		[Property]
		public bool IsConvertFormat { get; set; }

		[Property]
		public bool ParseWaybills { get; set; }

		[Property]
		public bool OnlyParseWaybills { get; set; }


		public bool ShouldParseWaybill()
		{
			return ParseWaybills || OnlyParseWaybills;
		}
	}

	public enum DocType
	{
		[Description("Накладная")] Waybill = 1,
		[Description("Отказ")] Reject = 2
	}

	[ActiveRecord("DocumentHeaders", Schema = "documents")]
	public class Document : ActiveRecordLinqBase<Document>
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(WaybillService));
		private readonly int _batchSize = 100;

		public Document()
		{}

		public Document(DocumentReceiveLog log)
		{
			Log = log;
			WriteTime = DateTime.Now;
			FirmCode = Convert.ToUInt32(log.Supplier.Id);
			ClientCode = log.ClientCode.Value;
			AddressId = log.AddressId;
			DocumentType = DocType.Waybill;
		}
		
		private int GetCount(int batchSize, int index)
		{
			return (batchSize + index) <= Lines.Count ? batchSize + index : Lines.Count - batchSize;
		}

		protected List<T> GetListSynonymFromDb<T>(List<string> synonyms, List<uint> priceCodes)
		{
			var criteriaSynonym = DetachedCriteria.For<T>();
			criteriaSynonym.Add(Restrictions.In("Synonym", synonyms));
			criteriaSynonym.Add(Restrictions.In("PriceCode", priceCodes));
			return SessionHelper.WithSession(c => criteriaSynonym.GetExecutableCriteria(c).List<T>()).ToList();
		}

		///<summary>
		/// сопоставление в накладной названию продуктов ProductId.
		/// </summary>
		/// 
		public Document SetProductId()
		{
			try
			{
				// получаем Id прайсов, из которых мы будем брать синонимы.
				var priceCodes = Price.Queryable
									.Where(p => (p.Supplier.Id == FirmCode))
									.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();

				if (priceCodes == null || priceCodes.Count <= 0)
					return this;

				// задаем количество строк, которое мы будем выбирать из списка продуктов в накладной.
				// Если накладная большая, то будем выбирать из неё продукты блоками.
				int realBatchSize = Lines.Count > _batchSize ? _batchSize : Lines.Count;
				int index = 0;
				int count = GetCount(realBatchSize, index);

				while ((count + index <= Lines.Count) && (count > 0))
				{
					// выбираем из накладной часть названия продуктов.
					var synonyms = Lines.ToList().GetRange(index, count).Select(i => i.Product).ToList();

					//выбираем из накладной часть названия производителей.
					var synonymsFirm = Lines.ToList().GetRange(index, count).Select(i => i.Producer).ToList();

					//получаем из базы данные для выбранной части продуктов из накладной.
					var dbListSynonym = GetListSynonymFromDb<SynonymProduct>(synonyms, priceCodes);
					//получаем из базы данные для выбранной части производителей из накладной.
					var dbListSynonymFirm = GetListSynonymFromDb<SynonymFirm>(synonymsFirm, priceCodes);
				
					//заполняем ProductId для продуктов в накладной по данным полученным из базы.
					foreach (var line in Lines)
					{
						var productName = line.Product;
						var producerName = line.Producer;
						var listSynonym = dbListSynonym.Where(product => product.Synonym == productName && product.ProductId != null).ToList();
						var listSynonymFirmCr = dbListSynonymFirm.Where(producer => producer.Synonym == producerName && producer.CodeFirmCr != null).ToList();

						//line.ProductId = listSynonym.Where(product => product.ProductId != null).Select(product => product.ProductId).Single();
						//line.ProducerId = listSynonymFirmCr.Where(producer => producer.CodeFirmCr != null).Select(producer => producer.CodeFirmCr).Single();
						if(listSynonym.Count > 0)
							line.ProductId = listSynonym.Select(product => product.ProductId).Single();
						if(listSynonymFirmCr.Count > 0)
							line.ProducerId = listSynonymFirmCr.Select(producer => producer.CodeFirmCr).Single();

						if (listSynonym.Count > 1)
							_log.Info(string.Format("В накладной при сопоставлении названия продукта оказалось более одного ProductId для продукта: {0}, FirmCode = {1}, ClientCode = {2}, Log.FileName = {3}, Log.Id = {4}", productName, FirmCode, ClientCode, Log.FileName, Log.Id));
						if(listSynonymFirmCr.Count > 1)
							_log.Info(string.Format("В накладной при сопоставлении названия производителя оказалось более одного ProducerId для производителя: {0}, FirmCode = {1}, ClientCode = {2}, Log.FileName = {3}, Log.Id = {4}", producerName, FirmCode, ClientCode, Log.FileName, Log.Id));																		
					}

					index = count;
					count = GetCount(realBatchSize, index);
				}
			}
			catch (Exception e)
			{
				_log.Error(String.Format("Ошибка при сопоставлении id синонимам в накладной {0}", Log.FileName), e);				
			}
			return this;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public DateTime WriteTime { get; set; }

		[Property]
		public uint FirmCode { get; set; }

		[Property]
		public uint ClientCode { get; set; }

		[Property]
		public uint? AddressId { get; set; }

		[Property]
		public DocType DocumentType { get; set; }

		[Property]
		public string ProviderDocumentId { get; set; }

		[Property]
		public DateTime? DocumentDate { get; set; }

		[Property]
		public string Parser { get; set; }

		[Property]
		public uint? OrderId { get; set; }

		[BelongsTo("DownloadId")]
		public DocumentReceiveLog Log { get; set; }

		[HasMany(ColumnKey = "DocumentId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<DocumentLine> Lines { get; set; }

		public DocumentLine NewLine()
		{
			return NewLine(new DocumentLine());
		}

		public DocumentLine NewLine(DocumentLine line)
		{
			if (Lines == null)
				Lines = new List<DocumentLine>();

			line.Document = this;
			Lines.Add(line);
			return line;
		}

		public void Parse(IDocumentParser parser, string file)
		{
			Parser = parser.GetType().Name;
			parser.Parse(file, this);
			if (!DocumentDate.HasValue)
				DocumentDate = DateTime.Now;
		}

		public static string GenerateProviderDocumentId()
		{
			return DateTime.Now.ToString()
				.Replace(".", String.Empty)
				.Replace(" ", String.Empty)
				.Replace(":", String.Empty)
				.Replace(",", String.Empty)
				.Replace("-", String.Empty)
				.Replace("/", String.Empty);
		}
	}

	[ActiveRecord("DocumentBodies", Schema = "documents")]
	public class DocumentLine
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("DocumentId")]
		public Document Document { get; set; }

		/// <summary>
		/// Id продукта
		/// </summary>
		[Property]
		public int? ProductId { get; set; }
		
		/// <summary>
		/// Наименование продукта
		/// </summary>
		[Property]
		public string Product { get; set; }

		/// <summary>
		/// Код товара
		/// </summary>
		[Property]
		public string Code { get; set; }

		/// <summary>
		/// Информация о сертификате это строка что то вроде РОСС.NL.ФМ09.Д00778
		/// </summary>
		[Property]
		public string Certificates { get; set; }

		/// <summary>
		/// Дата сертификата(в формате DD.MM.YYYY)
		/// </summary>
		//[Property]
		//public DateTime CertificatesDate { get; set; }
		
		/// <summary>
		/// Срок годности
		/// </summary>
		[Property]
		public string Period { get; set; }

		/// <summary>
		/// Id производителя
		/// </summary>
		[Property]
		public int? ProducerId { get; set; }

		/// <summary>
		/// Производитель
		/// </summary>
		[Property]
		public string Producer { get; set; }

		/// <summary>
		/// Страна производителя
		/// </summary>
		[Property]
		public string Country { get; set; }

		/// <summary>
		/// Цена производителя без НДС
		/// </summary>
		[Property]
		public decimal? ProducerCost { get; set; }

		/// <summary>
		/// Цена государственного реестра
		/// </summary>
		[Property]
		public decimal? RegistryCost { get; set; }

		/// <summary>
		/// Наценка поставщика
		/// </summary>
		[Property]
		public decimal? SupplierPriceMarkup { get; set; }

		/// <summary>
		/// Ставка налога на добавленную стоимость
		/// </summary>
		[Property]
		public uint? Nds { get; set; }

		/// <summary>
		/// Цена поставщика без НДС
		/// </summary>
		[Property]
		public decimal? SupplierCostWithoutNDS { get; set; }

		/// <summary>
		/// Цена поставщика с НДС
		/// </summary>
		[Property]
		public decimal? SupplierCost { get; set; }

		/// <summary>
		/// Количество
		/// </summary>
		[Property]
		public uint? Quantity { get; set; }

		/// <summary>
		/// Признак ЖНВЛС
		/// </summary>
		[Property]
		public bool? VitallyImportant { get; set; }

		/// <summary>
		/// Серийный номер продукта
		/// </summary>
		[Property]
		public string SerialNumber { get; set; }

		/// <summary>
		/// Сумма НДС
		/// </summary>
		[Property]
		public decimal? NdsAmount { get; set; }

		/// <summary>
		/// Сумма с НДС
		/// </summary>
		[Property]
		public decimal? Amount { get; set; }

		public void SetAmount()
		{
			if(!Amount.HasValue && SupplierCost.HasValue && Quantity.HasValue)
				Amount = SupplierCost*Quantity;
		}

		public void SetNdsAmount()
		{
			if (!NdsAmount.HasValue && SupplierCost.HasValue && 
				SupplierCostWithoutNDS.HasValue && Quantity.HasValue)
			{
				NdsAmount = Math.Round((SupplierCost.Value - SupplierCostWithoutNDS.Value) * Quantity.Value, 2);
			}
		}

		public void SetValues()
		{
			if (!SupplierCostWithoutNDS.HasValue && !Nds.HasValue && SupplierCost.HasValue && Quantity.HasValue)
				SetSupplierCostWithoutNds();
			if (!Nds.HasValue && SupplierCostWithoutNDS.HasValue)
				SetSupplierCostWithoutNds(SupplierCostWithoutNDS.Value);
			if (!SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SetNds(Nds.Value);
			if (!SupplierCost.HasValue && Nds.HasValue && SupplierCostWithoutNDS.HasValue)
				SetSupplierCostByNds(Nds.Value);
			
			SetAmount();
			SetNdsAmount();
		}

		public void SetNds(decimal nds)
		{
			if (SupplierCost.HasValue && !SupplierCostWithoutNDS.HasValue)
				SupplierCostWithoutNDS = Math.Round(SupplierCost.Value / (1 + nds / 100), 2);
			else if (!SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue)
				SupplierCost = Math.Round(SupplierCostWithoutNDS.Value * (1 + nds / 100), 2);
			Nds = (uint?) nds;
		}

		public void SetSupplierCostWithoutNds()
		{
			if (SupplierCost.HasValue && NdsAmount.HasValue && Quantity.HasValue &&
				!SupplierCostWithoutNDS.HasValue)
			{
				SupplierCostWithoutNDS = Math.Round(SupplierCost.Value - (NdsAmount.Value/Quantity.Value), 2);
			}
		}

		public void SetSupplierCostWithoutNds(decimal cost)
		{
			SupplierCostWithoutNDS = cost;
			Nds = null;
			if (SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue && (SupplierCostWithoutNDS.Value != 0))
			{
				decimal nds = (Math.Round((SupplierCost.Value/SupplierCostWithoutNDS.Value - 1)*100));
				Nds = nds < 0 ? 0 : (uint?)nds;
			}
			
		}

		public void SetSupplierCostByNds(decimal? nds)
		{
			Nds = (uint?) nds;
			SupplierCost = null;
			if (SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SupplierCost = Math.Round(SupplierCostWithoutNDS.Value*(1 + ((decimal) Nds.Value/100)), 2);
		}

		public void SetSupplierPriceMarkup()
		{
			if (!SupplierPriceMarkup.HasValue && ProducerCost.HasValue
				&& SupplierCostWithoutNDS.HasValue && (ProducerCost.Value != 0))
			{
				SupplierPriceMarkup = null;
				SupplierPriceMarkup = Math.Round(((SupplierCostWithoutNDS.Value/ProducerCost.Value - 1)*100), 2);
			}
		}
	}

	[ActiveRecord("SynonymFirmCr", Schema = "farm")]
	public class SynonymFirm : ActiveRecordLinqBase<SynonymFirm>
	{
		/// <summary>
		/// Id Синонима. Ключевое поле.
		/// </summary>
		[PrimaryKey]
		public int SynonymFirmCrCode { get; set; }

		/// <summary>
		/// Синоним производителя
		/// </summary>
		[Property]
		public string Synonym { get; set; }

		/// <summary>
		/// Код прайса
		/// </summary>
		[Property]
		public int? PriceCode { get; set; }

		/// <summary>
		/// Код производителя ProducerId
		/// </summary>
		[Property]
		public int? CodeFirmCr { get; set; }
	}

	[ActiveRecord("Synonym", Schema = "farm")]
	public class SynonymProduct : ActiveRecordLinqBase<SynonymProduct>
	{
		/// <summary>
		/// Id Синонима. Ключевое поле.
		/// </summary>
		[PrimaryKey]
		public int SynonymCode { get; set; }

		/// <summary>
		/// Id продукта
		/// </summary>
		//[BelongsTo("ProductId")]
		[Property]
		public int? ProductId { get; set; }

		/// <summary>
		/// Синоним продукта
		/// </summary>
		[Property]
		public string Synonym { get; set; }

		/// <summary>
		/// Код прайса
		/// </summary>
		[Property]
		public int? PriceCode { get; set; }
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
			
			//пробежать по logs и поставить Fake документам
			/*using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				logs.Each(l =>
				          	{
								if (WaybillSettings.Find(l.ClientCode.Value).IsConvertFormat)
								{
									l.IsFake = true;
									l.SaveAndFlush();
								}
				          	});
				scope.VoteCommit();
			}*/

			var docsForParsing = MultifileDocument.Merge(logs);

			var docs = docsForParsing.Select(d => {
				
				try
				{
					var settings = WaybillSettings.Find(d.DocumentLog.ClientCode.Value);
					
					if (d.DocumentLog.DocumentType == DocType.Reject)
						return null;
					if (shouldCheckClientSettings && !settings.ShouldParseWaybill())
						return null;
					
					var doc = detector.DetectAndParse(d.DocumentLog, d.FileName);
					
					// для мульти файла, мы сохраняем в источнике все файлы, 
					// а здесь, если нужна накладная в dbf формате, то сохраняем merge-файл в dbf формате.
					if (settings.IsConvertFormat)
					{
						ConvertAndSaveDbfFormat(doc, d.DocumentLog, true);
						//doc.Log.IsFake = true;
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
								d.Log.Save();
				               	d.Save();
				});
				scope.VoteCommit();
			}
			return docs;
		}

		public static void SaveWaybill(string filename)
		{
			if (!Directory.Exists(Settings.Default.FTPOptBoxPath))
				Directory.CreateDirectory(Settings.Default.FTPOptBoxPath);

			if (File.Exists(filename))
				File.Copy(filename, Path.Combine(Settings.Default.FTPOptBoxPath, Path.GetFileName(filename)), true);
		}

		public static void ParserDocument(DocumentReceiveLog log)
		{
			var file = log.GetFileName();

			try
			{
				//using(new SessionScope())
			//	using (var transaction = new TransactionScope(OnDispose.Rollback))
			//	{
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
						ConvertAndSaveDbfFormat(document, log, true);
						//log.IsFake = true;
						//SetIsFakeInDocumentReceiveLog(log);
					}
					

					using (var transaction = new TransactionScope(OnDispose.Rollback))
					{
							document.Log.Save();
							document.Save();
							transaction.VoteCommit();
					}
				//}
			}
			catch(Exception e)
			{
				_log.Error(String.Format("Ошибка при разборе документа {0}", file), e);
				SaveWaybill(file);
			}
		}

		private static DataTable GetProductsName(string productIds)
		{
			var dataset = With.Connection(conection => MySqlHelper.ExecuteDataset(conection, string.Format(@"select Id,
(
select
	concat(CatalogNames.Name, ' ', CatalogForms.Form, ' ', 
  cast(GROUP_CONCAT(ifnull(PropertyValues.Value, '') 
                    order by Properties.PropertyName, PropertyValues.Value
						        SEPARATOR ', '
						        ) as char)) as Name
	from
		(
			catalogs.products,
			catalogs.catalog,
			catalogs.CatalogForms,
			catalogs.CatalogNames
		)
		left join catalogs.ProductProperties on ProductProperties.ProductId = Products.Id
		left join catalogs.PropertyValues on PropertyValues.Id = ProductProperties.PropertyValueId
		left join catalogs.Properties on Properties.Id = PropertyValues.PropertyId
	where
		products.Id = p.Id
	and catalog.Id = products.CatalogId
	and CatalogForms.Id = catalog.FormId
	and CatalogNames.Id = catalog.NameId
) as Name
from catalogs.Products p
where p.Id in ({0});",productIds)));
			
			return dataset.Tables.Count > 0 ? dataset.Tables[0] : null;
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
			                       		new DataColumn("kol_tov")
			                       	});

			var productIds = document.Lines.Where(p=> p.ProductId != null).Select(p => p.ProductId).ToArray();
			var productsname = (productIds.Length > 0) ? GetProductsName(string.Join(",", productIds)) : new DataTable();

			foreach (var line in document.Lines)
			{
				var productId = line.ProductId;
				var producerId = line.ProducerId;

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
				row["id_artis"] = productId;
				row["name_artis"] = productId != null ?  
														productsname
															.AsEnumerable()
															.Where(r => Convert.ToInt32(r["Id"]) == productId).Select(r => r["Name"].ToString())
															.SingleOrDefault() 
														: "";
				row["przv_artis"] = producerId != null ? With.Connection(c => 
																		MySqlHelper.ExecuteScalar(c, "select Name from catalogs.Producers where Id = " + producerId).ToString()
																		) 
														:	"";
				row["name_post"] = line.Product;
				row["przv_post"] = line.Producer;
				row["seria"] = line.SerialNumber;
				row["sgodn"] = line.Period;
				row["sert"] = line.Certificates;
				row["sert_date"] = "";
				row["prcena_bnds"] = line.ProducerCost;
				row["gr_cena"] = line.RegistryCost;
				row["pcena_bnds"] = line.SupplierCostWithoutNDS;
				row["nds"] = line.Nds;
				row["pcena_nds"] = line.SupplierCost;
				row["kol_tov"] = line.Quantity;
				
				table.Rows.Add(row);
			}

			return table;
		}
		
		public static void ConvertAndSaveDbfFormat(Document document, DocumentReceiveLog log, bool isfake = false)
		{
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
		
/*		//ставим оригинальному файлу, что он fake, чтобы он не загружался.
		public static void SetIsFakeInDocumentReceiveLog(DocumentReceiveLog log)
		{
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				log.IsFake = true;
				log.SaveAndFlush();
				scope.VoteCommit();
			}
		}*/
	}
}
