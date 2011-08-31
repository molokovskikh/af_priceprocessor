﻿using System;
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
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Helpers;
using log4net;
using MySql.Data.MySqlClient;
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
	
    [ActiveRecord("Suppliers", Schema = "Future")]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
        [PrimaryKey]
		public uint Id { get; set; }

		[Property]
        public string Name { get; set; }
		
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
		public int? AssortimentPriceId { get; set; }

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

	public class AssortimentPriceInfo
	{
		public uint? Code { get; set; }
		public string Synonym { get; set; }
		public string SynonymFirmCr { get; set; }
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
					var productNames = Lines.ToList().GetRange(index, count).Select(line => line.Product.Trim().RemoveDoubleSpaces()).ToList();
				    //выбираем из накладной часть названия производителей.
					var firmNames = Lines.ToList().GetRange(index, count).Select(i => i.Producer.Trim().RemoveDoubleSpaces()).ToList();
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
	}

    [ActiveRecord("InvoiceHeaders", Schema = "documents")]
    public class Invoice: ActiveRecordLinqBase<Invoice>
    {
        [PrimaryKey(PrimaryKeyType.Foreign)]
        public uint Id { get; set; }

        [OneToOne]
        public Document Document { get; set; }

        /// <summary>
        /// Номер счет-фактуры
        /// </summary>
        [Property]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Дата счет-фактуры
        /// </summary>
        [Property]
        public DateTime? InvoiceDate { get; set; }

        /// <summary>
        /// Наименование продавца
        /// </summary>
        [Property]
        public string SellerName { get; set; }

        /// <summary>
        /// Адрес продавца
        /// </summary>
        [Property]
        public string SellerAddress { get; set; }

        /// <summary>
        /// ИНН продавца
        /// </summary>
        [Property]
        public string SellerINN { get; set; }

        /// <summary>
        /// КПП продавца
        /// </summary>
        [Property]
        public string SellerKPP { get; set; }

        /// <summary>
        /// Грузоотправитель и его адрес
        /// </summary>
        [Property]
        public string ShipperInfo { get; set; }

        /// <summary>
        /// Грузополучатель и его адрес
        /// </summary>
        [Property]
        public string ConsigneeInfo { get; set; }

        /// <summary>
        /// Поле К платежно-расчетному документу N
        /// </summary>
        [Property]
        public string PaymentDocumentInfo { get; set; }
        
        /// <summary>
        /// Наименование покупателя
        /// </summary>
        [Property]
        public string BuyerName { get; set; }

        /// <summary>
        /// Адрес покупателя
        /// </summary>
        [Property]
        public string BuyerAddress { get; set; }

        /// <summary>
        /// ИНН покупателя
        /// </summary>
        [Property]
        public string BuyerINN { get; set; }

        /// <summary>
        /// КПП покупателя
        /// </summary>
        [Property]
        public string BuyerKPP { get; set; }

        /// <summary>
        /// Стоимость товаров без налога для группы товаров, облагаемых ставкой 0% НДС
        /// </summary>
        [Property]
        public decimal? AmountWithoutNDS0 { get; set; }

        /// <summary>
        /// Стоимость товаров без налога для группы товаров, облагаемых ставкой 10% НДС
        /// </summary>
        [Property]
        public decimal? AmountWithoutNDS10 { get; set; }

        /// <summary>
        /// Сумма налога для группы товаров, облагаемых ставкой 10% НДС
        /// </summary>
        [Property]
        public decimal? NDSAmount10 { get; set; }

        /// <summary>
        /// Стоимость товаров для группы товаров, облагаемых ставкой 10% НДС всего с учётом налога
        /// </summary>
        [Property]
        public decimal? Amount10 { get; set; }

        /// <summary>
        /// Стоимость товаров без налога для группы товаров, облагаемых ставкой 18% НДС
        /// </summary>
        [Property]
        public decimal? AmountWithoutNDS18 { get; set; }

        /// <summary>
        /// Сумма налога для группы товаров, облагаемых ставкой 18% НДС
        /// </summary>
        [Property]
        public decimal? NDSAmount18 { get; set; }

        /// <summary>
        /// Стоимость товаров для группы товаров , облагаемых ставкой 18% НДС всего с учётом налога
        /// </summary>
        [Property]
        public decimal? Amount18 { get; set; }

        /// <summary>
        /// Общая стоимость товаров без налога (указывается в конце таблицы счёт-фактуры по строке «ИТОГО»)
        /// </summary>
        [Property]
        public decimal? AmountWithoutNDS { get; set; }

        /// <summary>
        /// Общая сумма налога (указывается в конце таблицы счёт-фактуры по строке «ИТОГО»)
        /// </summary>
        [Property]
        public decimal? NDSAmount { get; set; }

        /// <summary>
        /// Общая стоимость товаров с налогом (указывается в конце таблицы счёт-фактуры по строке «ИТОГО»)
        /// </summary>
        [Property]
        public decimal? Amount { get; set; }

		private int ToIntX100(decimal val)
		{
			return Convert.ToInt32(Math.Round(val, 2)*100);
		}

		public void CalculateValues()
		{
			if (!AmountWithoutNDS0.HasValue && AmountWithoutNDS.HasValue && AmountWithoutNDS10.HasValue && AmountWithoutNDS18.HasValue)
			{
				if (ToIntX100(AmountWithoutNDS10.Value) + ToIntX100(AmountWithoutNDS18.Value) <= ToIntX100(AmountWithoutNDS.Value))
					AmountWithoutNDS0 = Math.Round(AmountWithoutNDS.Value, 2) - Math.Round(AmountWithoutNDS10.Value, 2) - Math.Round(AmountWithoutNDS18.Value, 2);
			}
			if(!AmountWithoutNDS0.HasValue)
			{
				AmountWithoutNDS0 = Math.Round(Document.Lines.Where(l => l.Nds.HasValue && l.Nds.Value == 0 && l.SupplierCostWithoutNDS.HasValue && 
															l.Quantity.HasValue).Sum(l => l.SupplierCostWithoutNDS*l.Quantity).Value, 2);
			}
			if (!AmountWithoutNDS10.HasValue && NDSAmount10.HasValue && Amount10.HasValue)
			{
				if (ToIntX100(NDSAmount10.Value) <= ToIntX100(Amount10.Value))
					AmountWithoutNDS10 = Amount10 - NDSAmount10;
			}
			if (!AmountWithoutNDS10.HasValue && AmountWithoutNDS.HasValue && AmountWithoutNDS18.HasValue)
			{
				if (ToIntX100(AmountWithoutNDS0.Value) + ToIntX100(AmountWithoutNDS18.Value) <= ToIntX100(AmountWithoutNDS.Value))
					AmountWithoutNDS10 = Math.Round(AmountWithoutNDS.Value, 2) - Math.Round(AmountWithoutNDS0.Value, 2) - Math.Round(AmountWithoutNDS18.Value, 2);
			}			
			if(!AmountWithoutNDS10.HasValue)
			{
				AmountWithoutNDS10 = Math.Round(Document.Lines.Where(l => l.Nds.HasValue && l.Nds.Value == 10 && l.SupplierCostWithoutNDS.HasValue &&
															l.Quantity.HasValue).Sum(l => l.SupplierCostWithoutNDS * l.Quantity).Value, 2);
			}
			if (!AmountWithoutNDS18.HasValue && NDSAmount18.HasValue && Amount18.HasValue)
			{
				if (ToIntX100(NDSAmount18.Value) <= ToIntX100(Amount18.Value))
					AmountWithoutNDS18 = Math.Round(Amount18.Value, 2) - Math.Round(NDSAmount18.Value, 2);
			}
			if(!AmountWithoutNDS18.HasValue && AmountWithoutNDS.HasValue)
			{
				if (ToIntX100(AmountWithoutNDS10.Value) + ToIntX100(AmountWithoutNDS0.Value) <= ToIntX100(AmountWithoutNDS.Value))
					AmountWithoutNDS18 = Math.Round(AmountWithoutNDS.Value, 2) - Math.Round(AmountWithoutNDS0.Value, 2) - Math.Round(AmountWithoutNDS10.Value, 2);
			}			
			if(!AmountWithoutNDS18.HasValue)
			{
				AmountWithoutNDS18 = Math.Round(Document.Lines.Where(l => l.Nds.HasValue && l.Nds.Value == 18 && l.SupplierCostWithoutNDS.HasValue &&
															l.Quantity.HasValue).Sum(l => l.SupplierCostWithoutNDS * l.Quantity).Value, 2);	
			}
			if(!AmountWithoutNDS.HasValue && NDSAmount.HasValue && Amount.HasValue)
			{
				if (ToIntX100(NDSAmount.Value) <= ToIntX100(Amount.Value))
					AmountWithoutNDS = Math.Round(Amount.Value, 2) - Math.Round(NDSAmount.Value, 2);
			}
			if (!AmountWithoutNDS.HasValue)
				AmountWithoutNDS = AmountWithoutNDS0 + AmountWithoutNDS10 + AmountWithoutNDS18;

			if(!NDSAmount10.HasValue && Amount10.HasValue)
			{
				if (ToIntX100(AmountWithoutNDS10.Value) <= Amount10.Value)
					NDSAmount10 = Math.Round(Amount10.Value, 2) - Math.Round(AmountWithoutNDS10.Value, 2);
			}
			if (!NDSAmount10.HasValue && NDSAmount.HasValue && NDSAmount18.HasValue)
			{
				if(ToIntX100(NDSAmount18.Value) <= ToIntX100(NDSAmount.Value))
					NDSAmount10 = Math.Round(NDSAmount.Value, 2) - Math.Round(NDSAmount18.Value, 2);
			}
			if(!NDSAmount10.HasValue)
			{
				NDSAmount10 = Math.Round(Document.Lines.Where(l => l.Nds.HasValue && l.Nds.Value == 10 && l.NdsAmount.HasValue)
															.Sum(l => l.NdsAmount).Value, 2);
			}
			if(!NDSAmount18.HasValue && Amount18.HasValue)
			{
				if (ToIntX100(AmountWithoutNDS18.Value) <= ToIntX100(Amount18.Value))
					NDSAmount18 = Math.Round(Amount18.Value, 2) - Math.Round(AmountWithoutNDS18.Value, 2);
			}
			if(!NDSAmount18.HasValue && NDSAmount.HasValue)
			{
				if (ToIntX100(NDSAmount10.Value) <= ToIntX100(NDSAmount.Value))
					NDSAmount18 = Math.Round(NDSAmount.Value, 2) - Math.Round(NDSAmount10.Value, 2);
			}
			if(!NDSAmount18.HasValue)
			{
				NDSAmount18 = Math.Round(Document.Lines.Where(l => l.Nds.HasValue && l.Nds.Value == 18 && l.NdsAmount.HasValue)
															.Sum(l => l.NdsAmount).Value, 2);
			}
			if (!NDSAmount.HasValue)
				NDSAmount = NDSAmount10 + NDSAmount18;
			
			if (!Amount10.HasValue)
				Amount10 = NDSAmount10 + AmountWithoutNDS10;

			if (!Amount18.HasValue)
				Amount18 = NDSAmount18 + AmountWithoutNDS18;

			if (!Amount.HasValue)
				Amount = NDSAmount + AmountWithoutNDS;
		}
    }

	[ActiveRecord("Catalog", Schema = "Catalogs")]
	public class Catalog : ActiveRecordLinqBase<Catalog>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Pharmacie { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public bool Hidden { get; set; }
	}

	[ActiveRecord("Products", Schema = "Catalogs")]
	public class Product : ActiveRecordLinqBase<Product>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("CatalogId")]
		public virtual Catalog CatalogProduct { get; set; }

		[Property]
		public virtual bool Hidden { get; set; }
	}	
    
	[ActiveRecord("Assortment", Schema = "Catalogs")]
	public class Assortment : ActiveRecordLinqBase<Assortment>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("CatalogId")]
		public Catalog Catalog { get; set; }

		[Property]
		public uint ProducerId { get; set; }

		[Property]
		public bool Checked { get; set; }
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
		//[Property]
		//public int? ProductId { get; set; }
		/// <summary>
		/// Если не null, то содержит ссылку на сопоставленный продукт из catalogs.products
		/// </summary>
		[BelongsTo("ProductId")]
		public Product ProductEntity { get; set; }
		
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
		/// Дата сертификата
		/// </summary>
		[Property]
		public string CertificatesDate { get; set; }
		
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

        /// <summary>
        /// Единица измерения
        /// </summary>
        [Property]
        public string Unit { get; set; }

        /// <summary>
        /// В том числе акциз
        /// </summary>
        [Property]
        public decimal? ExciseTax { get; set; }

        /// <summary>
        /// № Таможенной декларации
        /// </summary>
        [Property]
        public string BillOfEntryNumber { get; set; }

        /// <summary>
        /// Код EAN-13 (штрих-код)
        /// </summary>
        [Property]
        public string EAN13 { get; set; }

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
		
		public void CalculateValues()
		{
			if (!SupplierCostWithoutNDS.HasValue && !Nds.HasValue && SupplierCost.HasValue && Quantity.HasValue)
				SetSupplierCostWithoutNds();
			if (!Nds.HasValue && SupplierCostWithoutNDS.HasValue)
				SetSupplierCostWithoutNds(SupplierCostWithoutNDS.Value);
			if (!SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SetNds(Nds.Value);
			if (!SupplierCost.HasValue && Nds.HasValue && SupplierCostWithoutNDS.HasValue)
				SetSupplierCostByNds(Nds.Value);
            if (!Nds.HasValue && !SupplierCost.HasValue && NdsAmount.HasValue && 
                Quantity.HasValue && Quantity > 0 && SupplierCostWithoutNDS.HasValue && SupplierCostWithoutNDS > 0)
			{
			    Nds = (uint?)Math.Round(NdsAmount.Value/Quantity.Value * 100 / SupplierCostWithoutNDS.Value);
			    SetSupplierCostByNds(Nds.Value);
			}
			if (!SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SetSupplierCostByNds(Nds.Value);
			if (SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue && Nds.HasValue)
			{
				if (Convert.ToInt32(Math.Round(SupplierCost.Value, 2) * 100) < Convert.ToInt32(Math.Round(SupplierCostWithoutNDS.Value, 2)*100))
					SetSupplierCostByNds(Nds.Value);
			}

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

		public AssortimentPriceInfo AssortimentPriceInfo { get; set; }		
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
		/// Прайс-лист
		/// </summary>
        [BelongsTo("PriceCode")]
        public Price Price { get; set; }

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
		/// Продукт
		/// </summary>
		[BelongsTo("ProductId")]
		public Product Product { get; set; }

        /// <summary>
        /// Уцененный
        /// </summary>
        [Property]
        public bool Junk { get; set; }

		/// <summary>
		/// Синоним продукта
		/// </summary>
		[Property]
		public string Synonym { get; set; }

		/// <summary>
		/// Прайс-лист
		/// </summary>		
        [BelongsTo("PriceCode")]
        public Price Price { get; set; }
	}

	[ActiveRecord("Core0", Schema = "Farm")]
	public class Core : ActiveRecordLinqBase<Core>
	{
		[PrimaryKey]
		public ulong Id { get; set; }

		[Property]
		public string Quantity { get; set; }

		[BelongsTo("PriceCode")]
		public Price Price { get; set; }

		[BelongsTo("SynonymCode")]
		public SynonymProduct ProductSynonym { get; set; }

		[BelongsTo("SynonymFirmCrCode")]
		public SynonymFirm ProducerSynonym { get; set; }

		[Property]
		public int? ProductId { get; set; }

		[Property]
		public int? CodeFirmCr { get; set; }

		[Property]
		public string Code { get; set; }
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
				row["prcena_bnds"] = line.ProducerCost;
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
