using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	[ActiveRecord("PricesData", Schema = "Usersettings")]
	public class Price : ActiveRecordLinqBase<Price>
	{
		[PrimaryKey("PriceCode")]
		public virtual uint Id { get; set; }

		[BelongsTo("FirmCode")]
		public virtual Supplier Supplier { get; set; }

		[Property]
		public virtual string PriceName { get; set; }
	}

	public class Customer
	{
		public string Id;
		public decimal PriceMarkup;
	}

	public class PriceXmlReader : IReader
	{
		private readonly string _filename;
		private readonly XmlReader _reader;
		private bool _inPrice;
		private bool _readed;

		public PriceXmlReader(string filename)
		{
			_filename = filename;
			var settings = new XmlReaderSettings {
				IgnoreWhitespace = true
			};
			_reader = XmlReader.Create(File.OpenRead(_filename), settings);
		}

		public IEnumerable<string> Prices()
		{
			while (ReadFromReader())
			{
				while (_reader.Name == "Прайс" && _reader.NodeType == XmlNodeType.Element && _readed)
				{
					_inPrice = true;
					_readed = false;
					yield return _reader.GetAttribute("ID");
				}
			}
		}

		public IEnumerable<Customer> Customers()
		{
			do
			{
				if (_reader.Name == "Получатель" && _reader.NodeType == XmlNodeType.Element)
				{
					yield return new Customer {
						Id = _reader.GetAttribute("ПолучательID"),
						PriceMarkup = Convert.ToDecimal(_reader.GetAttribute("Наценка"))
					};
				}
				else if (_reader.Name != "Получатель" && _reader.NodeType == XmlNodeType.Element)
				{
					yield break;
				}
			} while (ReadFromReader());
		}

		public IEnumerable<FormalizationPosition> Read()
		{
			var cost = CostDescriptions.First();
			while (ReadFromReader())
			{
				if (_inPrice && _reader.Name == "Позиция" && _reader.NodeType == XmlNodeType.Element)
				{
					yield return new FormalizationPosition {
						PositionName = _reader.GetAttribute("Наименование"),
						FirmCr = _reader.GetAttribute("Производитель"),
						Core = new NewCore {
							Code = _reader.GetAttribute("Код товара"),
							CodeCr = _reader.GetAttribute("Код производителя"),
							MaxBoundCost = GetNullable<decimal>("МаксЦена"),
							Quantity = _reader.GetAttribute("Количество"),
							Volume = _reader.GetAttribute("Упаковка"),
							Period = _reader.GetAttribute("СрокГодности"),
							RegistryCost = GetNullable<decimal>("ЦенаГР"),
							RequestRatio = GetNullable<uint>("Кратность"),
							OrderCost = GetNullable<decimal>("МинСумма"),
							MinOrderCount = GetNullable<uint>("МинКоличество"),
							Await = _reader.GetAttribute("Ожидаем") == "да",
							Junk = _reader.GetAttribute("Уцененно") == "да",
							VitallyImportant = _reader.GetAttribute("ЖВЛС") == "да",
							Costs = new[] {new Cost(cost, GetNullable<decimal>("Цена")),},
						}
					};
				}
				else if (_reader.Name != "Позиция" && _reader.NodeType == XmlNodeType.Element)
				{
					_inPrice = false;
					yield break;
				}
			}
		}

		private bool ReadFromReader()
		{
			_readed = true;
			return _reader.Read();
		}

		private T GetNullable<T>(string name)
		{
			var value = _reader.GetAttribute(name);
			if (String.IsNullOrWhiteSpace(value))
				return default(T);
			return (T)Convert.ChangeType(value, typeof (T), CultureInfo.InvariantCulture);
		}

		public List<CostDescription> CostDescriptions { get; set; }

		public void SendWaring(PriceLoggingStat stat)
		{}
	}

	public class FarmImpeksFormalizer : IPriceFormalizer
	{
		private string _fileName;
		private DataTable _data;
		private PriceFormalizationInfo _priceInfo;
		private ILog _log = LogManager.GetLogger(typeof (FarmImpeksFormalizer));

		public FarmImpeksFormalizer(string filename, MySqlConnection connection, DataTable data)
		{
			_fileName = filename;
			_data = data;
			_priceInfo = new PriceFormalizationInfo(data.Rows[0]);
		}

		public void Formalize()
		{
			var reader = new PriceXmlReader(_fileName);
			foreach (var priceName in reader.Prices())
			{
				var priceInfo = _data.Rows[0];
				var supplierId = Convert.ToUInt32(priceInfo["FirmCode"]);
				Price price;
				using(new SessionScope(FlushAction.Never))
					price = Price.Queryable.Where(p => p.Supplier.Id == supplierId && p.PriceName == priceName).FirstOrDefault();

				if (price == null)
				{
					_log.WarnFormat("Не смог найти прайс лист у поставщика {0} с именем '{1}', пропуская этот прайс", _priceInfo.FirmShortName, priceName);
					continue;
				}
				priceInfo["PriceCode"] = price.Id;
				var parser = new BasePriceParser2(reader, priceInfo);
				parser.Formalize();

				var customers = reader.Customers().ToList();
				With.Transaction((c, t) => {
					var command = new MySqlCommand(@"
update Future.Intersection i
set i.AvailableForClient = 0
where i.PriceId = ?priceId", c);
					command.Parameters.AddWithValue("?priceId", price.Id);
					command.ExecuteNonQuery();

					foreach (var customer in customers)
					{
						command.CommandText = @"
update Future.Intersection i
set i.AvailableForClient = 1, i.PriceMarkup = ?priceMarkup
where i.SupplierClientId = ?id and i.PriceId = ?priceId";
						command.Parameters.Clear();
						command.Parameters.AddWithValue("?id", customer.Id);
						command.Parameters.AddWithValue("?priceMarkup", customer.PriceMarkup);
						command.Parameters.AddWithValue("?priceId", price.Id);
						command.ExecuteNonQuery();
					}
				});
			}
		}

		public bool Downloaded { get; set; }
		public string InputFileName { get; set; }

		public int formCount
		{
			get { return 0; }
		}

		public int unformCount
		{
			get { return 0; }
		}

		public int zeroCount
		{
			get { return 0; }
		}

		public int forbCount
		{
			get { return 0; }
		}

		public int maxLockCount
		{
			get { return 0; }
		}

		public long priceCode
		{
			get { return _priceInfo.PriceCode; }
		}

		public long firmCode
		{
			get { return _priceInfo.FirmCode; }
		}

		public string firmShortName
		{
			get { return _priceInfo.FirmShortName; }
		}

		public string priceName
		{
			get { return _priceInfo.PriceName; }
		}
	}
}