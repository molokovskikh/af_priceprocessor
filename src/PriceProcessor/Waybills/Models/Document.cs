using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using Common.NHibernate;
using Dapper;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	[ActiveRecord("DocumentHeaders", Schema = "documents")]
	public class Document : ActiveRecordLinqBase<Document>
	{

		public class Barcode
		{
			public ulong Value { get; set; }
			public uint ProductId { get; set; }
			public uint ProducerId { get; set; }
		}

		private static readonly ILog _log = LogManager.GetLogger(typeof(WaybillService));

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
			DocumentDate = DateTime.Now;
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

		public bool SetAssortimentInfo(ISession session, WaybillSettings settings)
		{
			if (!settings.IsConvertFormat)
				return false;

			if (settings.AssortimentPriceId == null) {
				_log.InfoFormat("Не задан ассортиментный прайс-лист: ClientCode = {0}, Log.FileName = {1}, Log.Id = {2}", ClientCode, Log.FileName, Log.Id);
				return true;
			}

			// список id товаров из накладной
			var productIds = Lines.Where(l => l.ProductEntity != null).Select(l => (uint?)l.ProductEntity.Id).ToArray();
			var cores = session.Query<Offer>()
				.Where(x => x.Price.Id == settings.AssortimentPriceId.Value && productIds.Contains(x.ProductId))
				.ToList();
			foreach (var line in Lines) {
				//Сортируем по Code, чтобы каждый раз при сопоставлении выбирать одну и ту же позицию из позиций с одинаковыми ProductId и ProducerId
				//Предпочитаем предложения у которых совпадает цена реестра тк в прайсе могут быть позиции с разным кодом
				var core = cores.Where(c => line.ProductEntity != null && c.ProductId == line.ProductEntity.Id && c.CodeFirmCr == line.ProducerId)
					.OrderByDescending(x => x.RegistryCost == line.RegistryCost)
					.ThenBy(x => x.Code)
					.FirstOrDefault();
				if (core == null)
					continue;
				line.AssortimentPriceInfo = new AssortimentPriceInfo {
					Code = NullableConvert.ToUInt32(core.Code),
					CodeCr = NullableConvert.ToInt32(core.CodeCr),
					Synonym = core.ProductSynonym?.Synonym,
					SynonymFirmCr = core.ProducerSynonym?.Synonym
				};
			}
			return true;
		}

		///<summary>
		/// сопоставление в накладной названию продуктов ProductId.
		/// </summary>
		public void SetProductId()
		{
			try {
				SessionHelper.StartSession(SetProductId);
			}
			catch (Exception e) {
				_log.Error($"Ошибка при сопоставлении id синонимам в накладной {Log.FileName}", e);
			}
		}

		public void SetProductId(ISession session)
		{
			// получаем Id прайсов, из которых мы будем брать синонимы.
			var priceCodes = session.Query<Price>().Where(p => (p.Supplier.Id == FirmCode))
				.Select(p => (p.ParentSynonym ?? p.Id)).Distinct().ToList();
			if (priceCodes.Count <= 0)
				return;

			var products = Lines.Select(l => Normilize(l.Product))
				.Where(x => !String.IsNullOrEmpty(x))
				.Distinct()
				.ToList();
			var productSynonyms = session.Query<ProductSynonym>()
				.Where(x => priceCodes.Contains(x.Price.Id) && products.Contains(x.Synonym))
				.ToArray();

			var producers = Lines.Select(l => Normilize(l.Producer))
				.Where(x => !String.IsNullOrEmpty(x))
				.Distinct()
				.ToList();
			var producerSynonyms = session.Query<ProducerSynonym>()
				// && x.Producer != null - нужно игнорировать несопоставленные производители
				.Where(x => priceCodes.Contains(x.Price.Id) && producers.Contains(x.Synonym) && x.Producer != null)
				.ToArray();

			//выбираем из накладной коды
			var сodes = Lines.Select(x => Normilize(x.Code)).Where(x => !String.IsNullOrEmpty(x)).Distinct().ToArray();
			// получаем данные по кодам из базы
			var dbSupplierCodes = session
				.Query<SupplierCode>().Where(x => x.Supplier.Id == FirmCode && сodes.Contains(x.Code))
				.ToArray();

			//заполняем ProductId для продуктов в накладной по данным полученным из базы.
			foreach (var line in Lines) {
				var code = Normilize(line.Code);
				var codeCr = Normilize(line.CodeCr);
				var codeEntity = dbSupplierCodes
					.FirstOrDefault(x => !String.IsNullOrEmpty(code)
						&& Normilize(x.Code) == code
						&& Normilize(x.CodeCr) == codeCr
						&& x.Product != null);
				if (codeEntity != null) {
					// если нашли код, то сопоставляем и по продукту и по производителю
					line.ProductEntity = codeEntity.Product;
					line.ProducerId = codeEntity.ProducerId;
				}
				else {
					// если не удалось сопоставить по коду, то сопоставляем по наименованию
					var productName = Normilize(line.Product);
					var product = productSynonyms
						.Where(x => !String.IsNullOrEmpty(productName) && Normilize(x.Synonym) == productName && x.Product != null)
						.Select(x => x.Product)
						.FirstOrDefault();
					if (product == null)
						continue;
					line.ProductEntity = product;
					// если сопоставили позицию по продукту, сопоставляем по производителю
					var producerName = Normilize(line.Producer);
					var matched = producerSynonyms
						.Where(x => !String.IsNullOrEmpty(producerName) && Normilize(x.Synonym) == producerName && x.Producer != null)
						.ToList();
					if (matched.Count == 0)
						continue;

					if (!line.ProductEntity.CatalogProduct.Pharmacie) // не фармацевтика
						line.ProducerId = matched.Select(x => x.Producer.Id).FirstOrDefault();
					// если фармацевтика, то производителя ищем с учетом ассортимента
					else {
						var assortment = session.Query<Assortment>().Where(a => a.Catalog.Id == line.ProductEntity.CatalogProduct.Id).ToList();
						foreach (var producerSynonym in matched) {
							if (assortment.Any(a => a.ProducerId == producerSynonym.Producer.Id)) {
								line.ProducerId = producerSynonym.Producer.Id;
								break;
							}
						}
					}
				}
			}

			var targets = Lines.Where(x => x.EAN13 != null && (x.ProductEntity == null || x.ProducerId == null)).ToArray();
			if (targets.Length == 0)
				return;
			var barcodes = session.Connection.Query<Barcode>("select EAN13 as Value, ProductId, ProducerId from Catalogs.BarcodeProducts where EAN13 in @barcodes",
				new { barcodes = targets.Select(x => x.EAN13).ToList() })
				.ToList();
			foreach (var barcode in barcodes) {
				foreach (var line in targets.Where(x => x.EAN13 == barcode.Value)) {
					if (line.ProductEntity == null)
						line.ProductEntity = session.Load<Product>(barcode.ProductId);
					if (line.ProducerId == null)
						line.ProducerId = barcode.ProducerId;
				}
			}
		}


		private static string Normilize(string value)
		{
			return (value ?? "").Trim().ToUpper().RemoveDoubleSpaces();
		}

		public void CalculateValues()
		{
			Lines.Each(l => l.CalculateValues()); // расчет недостающих значений для позиций в накладной
			if (Invoice == null && HaveDataToInvoce())
				Invoice = new Invoice { Document = this };
			if (Invoice != null)
				Invoice.CalculateValues(); // расчет недостающих значений для счета-фактуры
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

		public void AddCertificateTask(DocumentLine documentLine, CertificateSource source)
		{
			if (source.IsDisabled)
				return;
			Tasks.Add(new CertificateTask(source, documentLine));
		}

		public void CreateCertificateTasks()
		{
			//если источник сертификатов достаточно медленный
			//то нужно проверять что задача не дубль
			Tasks.Where(t => !t.IsDuplicate()).Each(task => task.Save());
		}
	}
}