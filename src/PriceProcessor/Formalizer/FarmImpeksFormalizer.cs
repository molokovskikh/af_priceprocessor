using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
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

	public class PriceXmlReader : IReader
	{
		private readonly string _filename;
		private readonly XmlReader _reader;
		private bool _inPrice;

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
			while (_reader.Read())
			{
				if (_reader.Name == "Прайс" && _reader.NodeType == XmlNodeType.Element)
				{
					_inPrice = true;
					yield return _reader.GetAttribute("ID");
				}
			}
		}

		public IEnumerable<FormalizationPosition> Read()
		{
			var cost = CostDescriptions.First();
			while(_reader.Read())
			{
				if (_inPrice && _reader.Name == "Позиция")
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
				else if (_reader.NodeType != XmlNodeType.Element && _reader.Name != "Позиция")
				{
					_inPrice = false;
					yield break;
				}
			}
		}

		public T GetNullable<T>(string name)
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
				using(new SessionScope(FlushAction.Never))
				{
					var price = Price.Queryable.Where(p => p.Supplier.Id == supplierId && p.PriceName == priceName).FirstOrDefault();
					if (price == null)
					{
						_log.WarnFormat("Не смог найти прайс лист у поставщика {0} с именем '{1}', пропуская этот прайс", _priceInfo.FirmShortName, priceName);
						continue;
					}
					priceInfo["PriceCode"] = price.Id;
				}
				var parser = new BasePriceParser2(reader, priceInfo);
				parser.Formalize();
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