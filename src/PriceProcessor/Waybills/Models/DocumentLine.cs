using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	public class AssortimentPriceInfo
	{
		public uint? Code { get; set; }
		public string Synonym { get; set; }
		public string SynonymFirmCr { get; set; }
		public int? CodeCr { get; set; }
	}

	[ActiveRecord("ProtekDocs", Schema = "Documents")]
	public class ProtekDoc
	{
		public ProtekDoc()
		{
		}

		public ProtekDoc(DocumentLine line, int docId)
		{
			Line = line;
			DocId = docId;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo]
		public DocumentLine Line { get; set; }

		[Property]
		public int DocId { get; set; }
	}

	[ActiveRecord("DocumentBodies", Schema = "documents")]
	public class DocumentLine
	{
		public DocumentLine()
		{
			OrderItems = new List<OrderItem>();
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("DocumentId")]
		public Document Document { get; set; }

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
		/// Код товара поставщика
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
		/// Орган, выдавший документа качества
		/// </summary>
		[Property]
		public string CertificateAuthority { get; set; }

		/// <summary>
		/// Срок годности. А точнее Дата окончания срока годности.
		/// </summary>
		[Property]
		public string Period { get; set; }

		/// <summary>
		/// Срок годности в месяцах
		/// </summary>
		[Property]
		public int? ExpireInMonths { get; set; }

		/// <summary>
		/// Дата изготовления
		/// </summary>
		[Property]
		public DateTime? DateOfManufacture { get; set; }

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
		/// Код страны
		/// </summary>
		[Property]
		public string CountryCode { get; set; }

		/// <summary>
		/// Цена производителя без НДС
		/// </summary>
		[Property("ProducerCost")]
		public decimal? ProducerCostWithoutNDS { get; set; }

		/// <summary>
		/// Цена производителя с НДС (не маппится, используется для доп. расчетов)
		/// </summary>
		public decimal? ProducerCost { get; set; }

		/// <summary>
		/// Цена государственного реестра
		/// </summary>
		[Property]
		public decimal? RegistryCost { get; set; }

		/// <summary>
		/// Дата регистрации цены в ГосРеестре
		/// </summary>
		[Property]
		public DateTime? RegistryDate { get; set; }

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

		/// <summary>
		/// Код ОКДП
		/// </summary>
		[Property]
		public string CodeOKDP { get; set; }

		[BelongsTo(Column = "CertificateId", Lazy = FetchWhen.OnInvoke)]
		public Certificate Certificate { get; set; }

		/// <summary>
		/// Имя файла образа сертификата
		/// </summary>
		[Property]
		public string CertificateFilename { get; set; }

		/// <summary>
		/// Имя файла образа протокола
		/// </summary>
		[Property]
		public string ProtocolFilemame { get; set; }

		/// <summary>
		/// Имя файла образа паспорта
		/// </summary>
		[Property]
		public string PassportFilename { get; set; }

		/// <summary>
		/// ошибка говорящая о том почему не удалось загрузить сертификт
		/// </summary>
		[Property]
		public string CertificateError { get; set; }

		/// <summary>
		/// Оптовая цена
		/// </summary>
		[Property]
		public decimal? TradeCost { get; set; }

		/// <summary>
		/// Отпускная цена
		/// </summary>
		[Property]
		public decimal? SaleCost { get; set; }

		/// <summary>
		/// Розничная цена
		/// </summary>
		[Property]
		public decimal? RetailCost { get; set; }

		/// <summary>
		/// Шифр
		/// </summary>
		[Property]
		public string Cipher { get; set; }

		/// <summary>
		/// Код производителя
		/// </summary>
		[Property]
		public string CodeCr { get; set; }

		//список идентификаторов документов которые отдает протек
		//нужно для того что бы после разбора по этим идентификаторам загрузить файлы
		[HasMany(Cascade = ManyRelationCascadeEnum.All)]
		public IList<ProtekDoc> ProtekDocIds { get; set; }

		[HasAndBelongsToMany(Schema = "documents",
			Table = "waybillorders",
			ColumnKey = "DocumentLineId",
			ColumnRef = "OrderLineId",
			Lazy = true)]
		public IList<OrderItem> OrderItems { get; set; }

		public AssortimentPriceInfo AssortimentPriceInfo { get; set; }

		/// <summary>
		/// Номер заказа, которому соответствует данная позиция
		/// </summary>
		public uint? OrderId { get; set; }

		public const string EmptySerialNumber = "пустая серия";

		private bool StatedSerialNumber()
		{
			return !String.IsNullOrWhiteSpace(SerialNumber) && SerialNumber.Trim() != "-";
		}

		public string CertificateSerialNumber
		{
			get
			{
				if (StatedSerialNumber())
					return SerialNumber;

				return EmptySerialNumber;
			}
		}

		public void SetAmount()
		{
			if (!Amount.HasValue && SupplierCost.HasValue && Quantity.HasValue)
				Amount = SupplierCost * Quantity;
		}

		public void SetNdsAmount()
		{
			if (!NdsAmount.HasValue && SupplierCost.HasValue &&
				SupplierCostWithoutNDS.HasValue && Quantity.HasValue) {
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
				Quantity.HasValue && Quantity > 0 && SupplierCostWithoutNDS.HasValue && SupplierCostWithoutNDS > 0) {
				Nds = (uint?)Math.Round(NdsAmount.Value / Quantity.Value * 100 / SupplierCostWithoutNDS.Value);
				SetSupplierCostByNds(Nds.Value);
			}
			if (!SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SetSupplierCostByNds(Nds.Value);
			if (SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue && Nds.HasValue) {
				if (Convert.ToInt32(Math.Round(SupplierCost.Value, 2) * 100) < Convert.ToInt32(Math.Round(SupplierCostWithoutNDS.Value, 2) * 100))
					SetSupplierCostByNds(Nds.Value);
			}
			SetSupplierPriceMarkup();
			SetAmount();
			SetNdsAmount();
		}

		public void SetNds(decimal nds)
		{
			if (SupplierCost.HasValue && !SupplierCostWithoutNDS.HasValue)
				SupplierCostWithoutNDS = Math.Round(SupplierCost.Value / (1 + nds / 100), 2);
			else if (!SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue)
				SupplierCost = Math.Round(SupplierCostWithoutNDS.Value * (1 + nds / 100), 2);
			Nds = (uint?)nds;
		}

		public void SetSupplierCostWithoutNds()
		{
			if (SupplierCost.HasValue && NdsAmount.HasValue && Quantity.HasValue &&
				!SupplierCostWithoutNDS.HasValue) {
				SupplierCostWithoutNDS = Math.Round(SupplierCost.Value - (NdsAmount.Value / Quantity.Value), 2);
			}
		}

		public void SetSupplierCostWithoutNds(decimal cost)
		{
			SupplierCostWithoutNDS = cost;
			Nds = null;
			if (SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue && (SupplierCostWithoutNDS.Value != 0)) {
				decimal nds = (Math.Round((SupplierCost.Value / SupplierCostWithoutNDS.Value - 1) * 100));
				Nds = nds < 0 ? 0 : (uint?)nds;
			}
		}

		public void SetSupplierCostByNds(decimal? nds)
		{
			Nds = (uint?)nds;
			SupplierCost = null;
			if (SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SupplierCost = Math.Round(SupplierCostWithoutNDS.Value * (1 + ((decimal)Nds.Value / 100)), 2);
		}

		public void SetSupplierPriceMarkup()
		{
			if (!ProducerCostWithoutNDS.HasValue && !ProducerCostWithoutNDS.HasValue) return;
			if (!SupplierPriceMarkup.HasValue && ProducerCostWithoutNDS.HasValue
				&& SupplierCostWithoutNDS.HasValue && (ProducerCostWithoutNDS.Value != 0)) {
				SupplierPriceMarkup = null;
				SupplierPriceMarkup = Math.Round(((SupplierCostWithoutNDS.Value / ProducerCostWithoutNDS.Value - 1) * 100), 2);
			}
			else if (!SupplierPriceMarkup.HasValue && ProducerCost.HasValue
				&& SupplierCost.HasValue && (ProducerCost.Value != 0)) {
				SupplierPriceMarkup = Math.Round(((SupplierCost.Value / ProducerCost.Value - 1) * 100), 2);
			}
		}
	}
}