using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class Customer
	{
		public string SupplierClientId;
		public string SupplierPayerId;
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
		private ILog _log = LogManager.GetLogger(typeof (FarmaimpeksFormalizer));

		public FarmaimpeksFormalizer(string filename, MySqlConnection connection, DataTable data)
		{
			_fileName = filename;
			_data = data;
			_priceInfo = new PriceFormalizationInfo(data.Rows[0]);
		}

		public IList<string> GetAllNames()
		{
			var names = new List<string>();
			using (var reader = new FarmaimpeksReader(_fileName))
			{
				foreach (var parsedPrice in reader.Prices())
				{
					var priceInfo = _data.Rows[0];
					var supplierId = Convert.ToUInt32(priceInfo["FirmCode"]);
					PriceCost cost;
					using (new SessionScope(FlushAction.Never))
					{
						cost =
							PriceCost.Queryable.FirstOrDefault(
								c => c.Price.Supplier.Id == supplierId && c.CostName == parsedPrice.Id);
					}

					if (cost == null)
					{
						_log.WarnFormat(
							"Не смог найти прайс лист у поставщика {0} с именем '{1}', пропуская этот прайс",
							_priceInfo.FirmShortName, parsedPrice.Id);
						continue;
					}

					var info = new PriceFormalizationInfo(priceInfo);

					var parser = new BasePriceParser2(reader, info);
					parser.Downloaded = Downloaded;

					var ls = parser.GetAllNames();
					if (ls != null)
						names.AddRange(ls);
				}
			}
			return names;
		}

		public void Formalize()
		{
			using( var reader = new FarmaimpeksReader(_fileName)) {
				foreach (var parsedPrice in reader.Prices())
				{
					var supplierId = _priceInfo.FirmCode;
					PriceCost cost;
					using (new SessionScope(FlushAction.Never))
					{
						cost = PriceCost.Queryable.FirstOrDefault(c => c.Price.Supplier.Id == supplierId && c.CostName == parsedPrice.Id);
					}

					if (cost == null)
					{
						_log.WarnFormat("Не смог найти прайс лист у поставщика {0} с именем '{1}', пропуская этот прайс", _priceInfo.FirmShortName, parsedPrice.Id);
						continue;
					}

					FormalizePrice(reader, cost);

					var customers = reader.Settings().ToList();
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

						UpdateIntersection(command, cost, customers);
					});
				}
			}
		}
	}
}