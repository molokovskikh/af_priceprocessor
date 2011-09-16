﻿using System;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	public class AssortimentPriceInfo
	{
		public uint? Code { get; set; }
		public string Synonym { get; set; }
		public string SynonymFirmCr { get; set; }
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
}