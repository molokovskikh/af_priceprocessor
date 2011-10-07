using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using log4net;
using NHibernate.Criterion;

namespace Inforoom.PriceProcessor.Waybills.Models
{
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
			criteriaSynonym.Add(Restrictions.In("Price.Id", priceCodes));
			return SessionHelper.WithSession(c => criteriaSynonym.GetExecutableCriteria(c).List<T>()).ToList();
		}

		public bool SetAssortimentInfo()
		{
			bool result = false;
			try
			{				
				var settings = WaybillSettings.TryFind(ClientCode);
				if (settings == null) {
					_log.InfoFormat("Не найдены настройки: ClientCode = {0}, Log.FileName = {1}, Log.Id = {2}", ClientCode, Log.FileName, Log.Id);
					return false;
				}

				if (settings.AssortimentPriceId == null)
				{
					_log.InfoFormat("Не задан ассортиментный прайс-лист: ClientCode = {0}, Log.FileName = {1}, Log.Id = {2}", ClientCode, Log.FileName, Log.Id);
					return false;
				}

				// список id товаров из накладной				
				var productIds = Lines.Where(l => l.ProductEntity != null).Select(l => l.ProductEntity.Id).ToList();

				var criteria = DetachedCriteria.For<Core>();
				criteria.Add(Restrictions.Eq("Price.Id", (uint)settings.AssortimentPriceId.Value));
				criteria.Add(Restrictions.In("ProductId", productIds));

				List<Core> cores = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<Core>()).ToList();
				
				foreach (var line in Lines)
				{
					var ls = cores.Where(c => line.ProductEntity != null && c.ProductId == line.ProductEntity.Id && c.CodeFirmCr == line.ProducerId).ToList();
					if (ls.Count() > 0)
					{
						//Сортируем по Code, чтобы каждый раз при сопоставлении выбирать одну и ту же позицию из позиций с одинаковыми ProductId и ProducerId
						var core = ls.OrderBy(c => c.Code).FirstOrDefault();
						var info = new AssortimentPriceInfo();
						uint res;
						info.Code = UInt32.TryParse(core.Code, out res) ? (uint?)res : null;
						info.Synonym = core.ProductSynonym != null ? core.ProductSynonym.Synonym : null;
						info.SynonymFirmCr = core.ProducerSynonym != null ? core.ProducerSynonym.Synonym : null;
						line.AssortimentPriceInfo = info;
					}
				}
				result = true;
			}
			catch (Exception e)
			{
				_log.Error(String.Format("Ошибка при заполнении данных из ассортиментного прайс-листа для накладной {0}", Log.FileName), e);				
			}
			return result;
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
				var priceCodes = Price.Queryable.Where(p => (p.Supplier.Id == FirmCode))
												.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();
				if (priceCodes.Count <= 0 || Lines == null) return this;				
				// задаем количество строк, которое мы будем выбирать из списка продуктов в накладной.
				// Если накладная большая, то будем выбирать из неё продукты блоками.
				int realBatchSize = Lines.Count > _batchSize ? _batchSize : Lines.Count;
				int index = 0;
				int count = GetCount(realBatchSize, index);

				while ((count + index <= Lines.Count) && (count > 0))
				{				    
					// выбираем из накладной часть названия продуктов.
					var productNames = Lines.ToList().GetRange(index, count).Where(line => !String.IsNullOrEmpty(line.Product)).Select(line => line.Product.Trim().RemoveDoubleSpaces()).ToList();
					//выбираем из накладной часть названия производителей.
					var firmNames = Lines.ToList().GetRange(index, count).Where(line => !String.IsNullOrEmpty(line.Producer)).Select(line => line.Producer.Trim().RemoveDoubleSpaces()).ToList();
					//получаем из базы данные для выбранной части продуктов из накладной.
					var dbListSynonym = GetListSynonymFromDb<SynonymProduct>(productNames, priceCodes);
					//получаем из базы данные для выбранной части производителей из накладной.
					var dbListSynonymFirm = GetListSynonymFromDb<SynonymFirm>(firmNames, priceCodes);
				
					//заполняем ProductId для продуктов в накладной по данным полученным из базы.
					foreach (var line in Lines)
					{
						var productName = (String.IsNullOrEmpty(line.Product) == false ? line.Product.Trim().ToUpper() : String.Empty).RemoveDoubleSpaces();						
						var listSynonym = dbListSynonym.Where(syn => syn.Synonym.Trim().ToUpper() == productName && syn.Product != null).ToList();
						if(listSynonym.Count > 0)
						{
							line.ProductEntity = listSynonym.Select(syn => syn.Product).FirstOrDefault();
						}
						if (line.ProductEntity == null) continue; // если сопоставили позицию по продукту, сопоставляем по производителю
						var producerName = (String.IsNullOrEmpty(line.Producer) == false ? line.Producer.Trim().ToUpper() : String.Empty).RemoveDoubleSpaces();
						if(!line.ProductEntity.CatalogProduct.Pharmacie) // не фармацевтика
						{								
							var listSynonymFirmCr = dbListSynonymFirm.Where(syn => syn.Synonym.Trim().ToUpper() == producerName && syn.CodeFirmCr != null).ToList();
							if (listSynonymFirmCr.Count > 0)
								line.ProducerId = listSynonymFirmCr.Select(firmSyn => firmSyn.CodeFirmCr).FirstOrDefault();
						}
						else // если фармацевтика, то производителя ищем с учетом ассортимента
						{
							if (String.IsNullOrEmpty(producerName)) continue;
							var listSynonymFirmCr = dbListSynonymFirm.Where(producerSyn => producerSyn.Synonym.Trim().ToUpper() == producerName && producerSyn.CodeFirmCr != null).ToList();
							using(new SessionScope())
							{
								var assortment = Assortment.Queryable.Where(a => a.Catalog.Id == line.ProductEntity.CatalogProduct.Id).ToList();
								foreach (var producerSynonym in listSynonymFirmCr)
								{
									if(assortment.Any(a => a.ProducerId == producerSynonym.CodeFirmCr))
									{
										line.ProducerId = producerSynonym.CodeFirmCr;
										break;
									}
								}
							}
						}
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

		public  void CalculateValues() 
		{
			if(Lines != null) Lines.Each(l => l.CalculateValues()); // расчет недостающих значений для позиций в накладной
			if(Invoice != null) Invoice.CalculateValues(); // расчет недостающих значений для счета-фактуры
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

		[OneToOne(Cascade = CascadeEnum.All)]
		public Invoice Invoice { get; private set; }

		public Invoice SetInvoice()
		{
			if (Invoice == null)
			{
				Invoice = new Invoice();
				Invoice.Document = this;
			}
			return Invoice;
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

		public List<CertificateTask> Tasks = new List<CertificateTask>();

		public void AddCertificateTask(DocumentLine documentLine, CertificateSource certificateSource)
		{
			if (!Tasks.Exists(
					t => t.CatalogProduct.Id == documentLine.ProductEntity.CatalogProduct.Id 
						&& t.SerialNumber.Equals(documentLine.SerialNumber, StringComparison.CurrentCultureIgnoreCase)))
				Tasks.Add(new CertificateTask {
					CertificateSource = certificateSource,
					CatalogProduct = documentLine.ProductEntity.CatalogProduct,
					SerialNumber = documentLine.SerialNumber,
					DocumentLine = documentLine
				});
		}

		public void CreateCertificateTasks()
		{
			Tasks.ForEach(task => { 
				var existsTask = CertificateTask.Exists(
					DetachedCriteria.For<CertificateTask>()
						.Add(Restrictions.Eq("CertificateSource.Id", task.CertificateSource.Id))
						.Add(Restrictions.Eq("CatalogProduct.Id", task.CatalogProduct.Id))
						.Add(Restrictions.Eq("SerialNumber", task.SerialNumber)));

				if (!existsTask)
					task.Save();
				else {
					ActiveRecordMediator.Evict(task);
					_log.WarnFormat("Отклонено создании дубликата задачи на разбор сертификата: CatalogId: {0}  SerialNumber: {1}  DocumentBodyId: {2}", 
						task.CatalogProduct.Id, 
						task.SerialNumber,
						task.DocumentLine.Id);
				}
			});
		}

	}
}