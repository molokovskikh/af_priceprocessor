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
		{
			Lines = new List<DocumentLine>();
		}

		public Document(DocumentReceiveLog log, string parser = null)
			: this()
		{
			Parser = parser;
			Log = log;
			WriteTime = DateTime.Now;
			FirmCode = Convert.ToUInt32(log.Supplier.Id);
			ClientCode = log.ClientCode.Value;
			Address = log.Address;
			DocumentType = DocType.Waybill;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public DateTime WriteTime { get; set; }

		[Property]
		public uint FirmCode { get; set; }

		[Property]
		public uint ClientCode { get; set; }

		[BelongsTo("AddressId")]
		public Address Address { get; set; }

		[Property]
		public DocType DocumentType { get; set; }

		/// <summary>
		/// Номер накладной.
		/// </summary>
		[Property]
		public string ProviderDocumentId { get; set; }

		/// <summary>
		/// Дата накладной.
		/// </summary>
		[Property]
		public DateTime? DocumentDate { get; set; }

		[Property]
		public string Parser { get; set; }

		/// <summary>
		/// наш номер заявки, на основании кот. сформирована накладная
		/// </summary>
		[Property]
		public uint? OrderId { get; set; }

		[BelongsTo("DownloadId")]
		public DocumentReceiveLog Log { get; set; }

		[HasMany(ColumnKey = "DocumentId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<DocumentLine> Lines { get; set; }

		[OneToOne(Cascade = CascadeEnum.All)]
		public Invoice Invoice { get; private set; }

		public List<CertificateTask> Tasks = new List<CertificateTask>();

		private int GetCount(int batchSize, int index)
		{
			return (batchSize + index) <= Lines.Count ? batchSize : Lines.Count - index;
		}

		protected List<SupplierCode> GetSupplierCodesFromDb(List<string> codes, uint supplierId)
		{
			var criteriaSynonym = DetachedCriteria.For<SupplierCode>();
			criteriaSynonym.Add(Restrictions.In("Code", codes));
			criteriaSynonym.Add(Restrictions.Eq("Supplier.Id", supplierId));
			return SessionHelper.WithSession(c => criteriaSynonym.GetExecutableCriteria(c).List<SupplierCode>()).ToList();
		}

		protected List<T> GetListSynonymFromDb<T>(List<string> synonyms, List<uint> priceCodes, string colName = "Synonym")
		{
			var criteriaSynonym = DetachedCriteria.For<T>();
			criteriaSynonym.Add(Restrictions.In(colName, synonyms));
			criteriaSynonym.Add(Restrictions.In("Price.Id", priceCodes));
			return SessionHelper.WithSession(c => criteriaSynonym.GetExecutableCriteria(c).List<T>()).ToList();
		}

		public bool SetAssortimentInfo(WaybillSettings settings)
		{
			if (settings.AssortimentPriceId == null) {
				_log.InfoFormat("Не задан ассортиментный прайс-лист: ClientCode = {0}, Log.FileName = {1}, Log.Id = {2}", ClientCode, Log.FileName, Log.Id);
				return true;
			}

			// список id товаров из накладной
			var productIds = Lines.Where(l => l.ProductEntity != null).Select(l => l.ProductEntity.Id).ToList();

			var criteria = DetachedCriteria.For<Core>();
			criteria.Add(Restrictions.Eq("Price.Id", settings.AssortimentPriceId.Value));
			criteria.Add(Restrictions.In("ProductId", productIds));

			var cores = SessionHelper.WithSession(c => criteria.GetExecutableCriteria(c).List<Core>()).ToList();
			foreach (var line in Lines) {
				var ls = cores.Where(c => line.ProductEntity != null && c.ProductId == line.ProductEntity.Id && c.CodeFirmCr == line.ProducerId).ToList();
				if (ls.Any()) {
					//Сортируем по Code, чтобы каждый раз при сопоставлении выбирать одну и ту же позицию из позиций с одинаковыми ProductId и ProducerId
					var core = ls.OrderBy(c => c.Code).FirstOrDefault();
					var info = new AssortimentPriceInfo();
					uint res;
					info.Code = UInt32.TryParse(core.Code, out res) ? (uint?)res : null;
					info.CodeCr = core.CodeCr;
					info.Synonym = core.ProductSynonym != null ? core.ProductSynonym.Synonym : null;
					info.SynonymFirmCr = core.ProducerSynonym != null ? core.ProducerSynonym.Synonym : null;
					line.AssortimentPriceInfo = info;
				}
			}
			return true;
		}

		///<summary>
		/// сопоставление в накладной названию продуктов ProductId.
		/// </summary>
		/// 
		public Document SetProductId()
		{
			try {
				using (new SessionScope()) {
					// получаем Id прайсов, из которых мы будем брать синонимы.
					var priceCodes = Price.Queryable.Where(p => (p.Supplier.Id == FirmCode))
						.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();
					if (priceCodes.Count <= 0)
						return this;

					// задаем количество строк, которое мы будем выбирать из списка продуктов в накладной.
					// Если накладная большая, то будем выбирать из неё продукты блоками.
					int realBatchSize = Lines.Count > _batchSize ? _batchSize : Lines.Count;
					int index = 0;
					int count = GetCount(realBatchSize, index);

					while ((count + index <= Lines.Count) && (count > 0)) {
						// выбираем из накладной часть названия продуктов.
						var productNames =
							Lines.ToList().GetRange(index, count).Where(line => !String.IsNullOrEmpty(line.Product)).Select(
								line => line.Product.Trim().RemoveDoubleSpaces()).ToList();
						//выбираем из накладной часть названия производителей.
						var firmNames =
							Lines.ToList().GetRange(index, count).Where(line => !String.IsNullOrEmpty(line.Producer)).Select(
								line => line.Producer.Trim().RemoveDoubleSpaces()).ToList();
						//получаем из базы данные для выбранной части продуктов из накладной.
						var dbListSynonym = GetListSynonymFromDb<ProductSynonym>(productNames, priceCodes);
						//получаем из базы данные для выбранной части производителей из накладной.
						var dbListSynonymFirm = GetListSynonymFromDb<ProducerSynonym>(firmNames, priceCodes);
						//выбираем из накладной коды
						var сodes =
							Lines.ToList().GetRange(index, count).Where(line => !String.IsNullOrEmpty(line.Code)).Select(
								line => line.Code.Trim().RemoveDoubleSpaces()).ToList();

						// получаем данные по кодам из базы
						var dbSupplierCodes = GetSupplierCodesFromDb(сodes, FirmCode);

						//заполняем ProductId для продуктов в накладной по данным полученным из базы.
						foreach (var line in Lines) {
							var code = (String.IsNullOrEmpty(line.Code) == false ? line.Code.Trim().ToUpper() : String.Empty).RemoveDoubleSpaces();
							var codeCr = (String.IsNullOrEmpty(line.CodeCr) == false ? line.CodeCr.Trim().ToUpper() : String.Empty).RemoveDoubleSpaces();
							var listSupplierCode = dbSupplierCodes.Where(syn =>
								!String.IsNullOrEmpty(code)
									&& syn.Code.Trim().ToUpper() == code
									&& syn.CodeCr.Trim().ToUpper() == codeCr
									&& syn.Product != null)
								.ToList();
							if(listSupplierCode.Count > 0) {
								// если нашли код, то сопоставляем и по продукту и по производителю
								var firstSupplierCode = listSupplierCode.First();
								line.ProductEntity = firstSupplierCode.Product;
								line.ProducerId = firstSupplierCode.ProducerId;
							}
							else {
								// если не удалось сопоставить по коду, то сопоставляем по наименованию
								var productName =
									(String.IsNullOrEmpty(line.Product) == false ? line.Product.Trim().ToUpper() : String.Empty).RemoveDoubleSpaces();
								var listSynonym =
									dbListSynonym.Where(syn => !String.IsNullOrEmpty(productName) && syn.Synonym.Trim().ToUpper() == productName && syn.Product != null).ToList();
								if (listSynonym.Count == 0) continue;
								line.ProductEntity = listSynonym.Select(syn => syn.Product).FirstOrDefault();
								// если сопоставили позицию по продукту, сопоставляем по производителю
								var producerName = (String.IsNullOrEmpty(line.Producer) == false ? line.Producer.Trim().ToUpper() : String.Empty).RemoveDoubleSpaces();
								var listSynonymFirmCr =
									dbListSynonymFirm.Where(syn => !String.IsNullOrEmpty(producerName) && syn.Synonym.Trim().ToUpper() == producerName && syn.CodeFirmCr != null).ToList();
								if (listSynonymFirmCr.Count == 0) continue;

								if (!line.ProductEntity.CatalogProduct.Pharmacie) // не фармацевтика
									line.ProducerId = listSynonymFirmCr.Select(firmSyn => firmSyn.CodeFirmCr).FirstOrDefault();
									// если фармацевтика, то производителя ищем с учетом ассортимента
								else {
									var assortment = Assortment.Queryable.Where(a => a.Catalog.Id == line.ProductEntity.CatalogProduct.Id).ToList();
									foreach (var producerSynonym in listSynonymFirmCr) {
										if (assortment.Any(a => a.ProducerId == producerSynonym.CodeFirmCr)) {
											line.ProducerId = producerSynonym.CodeFirmCr;
											break;
										}
									}
								}
							}
						}
						index += count;
						count = GetCount(realBatchSize, index);
					}
				}
			}
			catch (Exception e) {
				_log.Error(String.Format("Ошибка при сопоставлении id синонимам в накладной {0}", Log.FileName), e);
			}
			return this;
		}

		public void CalculateValues()
		{
			Lines.Each(l => l.CalculateValues()); // расчет недостающих значений для позиций в накладной
			if (Invoice == null && HaveDataToInvoce())
				Invoice = new Invoice { Document = this };
			if (Invoice != null) Invoice.CalculateValues(); // расчет недостающих значений для счета-фактуры
		}

		public bool HaveDataToInvoce()
		{
			var result = Lines.All(l => l.Amount.HasValue) && Lines.All(l => l.NdsAmount.HasValue);
			return result;
		}

		public DocumentLine NewLine()
		{
			return NewLine(new DocumentLine());
		}

		public DocumentLine NewLine(DocumentLine line)
		{
			line.Document = this;
			Lines.Add(line);
			return line;
		}

		public Invoice SetInvoice()
		{
			if (Invoice == null) {
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

		public void AddCertificateTask(DocumentLine documentLine, CertificateSource certificateSource)
		{
			Tasks.Add(new CertificateTask(certificateSource, documentLine));
		}

		public void CreateCertificateTasks()
		{
			//если источник сертификатов достаточно медленный
			//то нужно проверять что задача не дубль
			Tasks.Where(t => !t.IsDuplicate()).Each(task => task.Save());
		}
	}
}