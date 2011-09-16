using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Waybills.Models
{
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
}