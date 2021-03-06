﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class Customer
	{
		public string SupplierClientId;
		public string SupplierPaymentId;
		public decimal? PriceMarkup;
		public string CostId;
		public bool? Available;
		public List<AddressSettings> Addresses = new List<AddressSettings>();
	}

	public class AddressSettings
	{
		public string SupplierAddressId;
		public bool? ControlMinReq;
		public uint? MinReq;
	}

	public class FarmaimpeksPrice
	{
		public string Id;
		public string Name;
	}

	public class FarmaimpeksFormalizer : BaseFormalizer, IPriceFormalizer
	{
		private ILog _log = LogManager.GetLogger(typeof(FarmaimpeksFormalizer));

		public FarmaimpeksFormalizer(string filename, PriceFormalizationInfo data)
			: base(filename, data)
		{
		}

		public IList<string> GetAllNames()
		{
			var names = new List<string>();
			using (var reader = new FarmaimpeksReader(_fileName)) {
				foreach (var parsedPrice in reader.Prices()) {
					var priceInfo = _data.Rows[0];
					var supplierId = Convert.ToUInt32(priceInfo["FirmCode"]);
					PriceCost cost;
					using (new SessionScope(FlushAction.Never)) {
						cost =
							PriceCost.Queryable.FirstOrDefault(
								c => c.Price.Supplier.Id == supplierId && c.CostName == parsedPrice.Id);
					}

					if (cost == null) {
						_log.WarnFormat(
							"Не смог найти прайс лист у поставщика {0} с именем '{1}', пропуская этот прайс",
							Info.FirmShortName, parsedPrice.Id);
						continue;
					}

					var info = new PriceFormalizationInfo(priceInfo, cost.Price);
					var parser = new BasePriceParser(reader, info);
					parser.Downloaded = Downloaded;
					names.AddRange(reader.Read()
						.Where(x => !String.IsNullOrWhiteSpace(x.PositionName)).Select(x => x.PositionName));
				}
			}
			return names;
		}

		public void Formalize()
		{
			using (var reader = new FarmaimpeksReader(_fileName)) {
				foreach (var parsedPrice in reader.Prices()) {
					var supplierId = Info.FirmCode;
					PriceCost cost;
					using (new SessionScope(FlushAction.Never)) {
						cost = PriceCost.Queryable.FirstOrDefault(c => c.Price.Supplier.Id == supplierId && c.CostName == parsedPrice.Id);
					}

					if (cost == null) {
						_log.WarnFormat("Не смог найти прайс лист у поставщика {0} с именем '{1}', пропуская этот прайс", Info.FirmShortName, parsedPrice.Id);
						continue;
					}

					Info.IsUpdating = true;
					Info.CostCode = cost.Id;
					Info.PriceItemId = cost.PriceItemId;
					Info.PriceCode = cost.Price.Id;

					FormalizePrice(reader);

					var customers = reader.Settings().ToList();

					//фармаимпекс не передает флаг доступности клиенту, подразумевается что если есть настройка
					//то прайс доступен
					foreach (var customer in customers)
						customer.Available = true;

					With.Transaction((c, t) => {
						var command = new MySqlCommand(@"
	update Usersettings.Pricesdata
	set PriceName = ?name
	where pricecode = ?PriceId", c);
						command.Parameters.AddWithValue("?PriceId", cost.Price.Id);
						command.Parameters.AddWithValue("?Name", parsedPrice.Name);
						command.ExecuteNonQuery();

						command = new MySqlCommand(@"
	update Customers.Intersection i
	set i.AvailableForClient = 0
	where i.PriceId = ?priceId", c);
						command.Parameters.AddWithValue("?priceId", cost.Price.Id);
						command.ExecuteNonQuery();

						UpdateIntersection(command, customers, reader.CostDescriptions);
					});
				}
			}
		}
	}
}