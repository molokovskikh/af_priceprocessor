using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using MySql.Data.MySqlClient;
using Castle.ActiveRecord.Framework;

namespace Inforoom.PriceProcessor.Formalizer
{
	[ActiveRecord("PricesData", Schema = "Usersettings", DynamicUpdate = true)]
	public class Price : ActiveRecordLinqBase<Price>
	{
		[PrimaryKey("PriceCode")]
		public virtual uint Id { get; set; }

		[BelongsTo("FirmCode")]
		public virtual Supplier Supplier { get; set; }

		[Property]
		public virtual string PriceName { get; set; }

		[Property]
		public virtual uint? ParentSynonym { get; set; }

		[HasMany(ColumnKey = "PriceCode", Inverse = true)]
		public virtual IList<PriceCost> Costs { get; set; }

		[HasMany(ColumnKey = "PriceCode", Cascade = ManyRelationCascadeEnum.AllDeleteOrphan, Lazy = true)]
		public virtual IList<SynonymProduct> ProductSynonyms { get; set; }

		[HasMany(ColumnKey = "PriceCode", Cascade = ManyRelationCascadeEnum.AllDeleteOrphan, Lazy = true)]
		public virtual IList<SynonymFirm> ProducerSynonyms { get; set; }

	}

	[ActiveRecord("PricesCosts", Schema = "Usersettings")]
	public class PriceCost : ActiveRecordLinqBase<PriceCost>
	{
		[PrimaryKey("CostCode")]
		public virtual uint Id { get; set;  }

		[BelongsTo("PriceCode")]
		public virtual Price Price { get; set; }

		[Property]
		public virtual uint PriceItemId { get; set; }

		[Property]
		public virtual string CostName { get; set; }
	}

	public class Customer
	{
		public string Id;
		public decimal PriceMarkup;
	}

	public class FarmaimpeksPrice
	{
		public string Id;
		public string Name;
	}


	public class PriceXmlReader : IReader, IDisposable
	{
		private readonly string _filename;
		private readonly XmlReader _reader;
		private bool _inPrice;
		private bool _readed;
		private Stream _stream;

		public PriceXmlReader(string filename)
		{
			_filename = filename;
			var settings = new XmlReaderSettings {
				IgnoreWhitespace = true
			};
			//_reader = XmlReader.Create(File.OpenRead(_filename), settings);
			_stream = File.OpenRead(_filename);
			_reader = XmlReader.Create(_stream, settings);
		}

		public void Dispose()
		{                
			_reader.Close();
			_stream.Close();
		}

		public IEnumerable<FarmaimpeksPrice> Prices()
		{
			while (ReadFromReader())
			{
				while (_reader.Name == "Прайс" && _reader.NodeType == XmlNodeType.Element && _readed)
				{
					_inPrice = true;
					_readed = false;
					yield return new FarmaimpeksPrice {
						Id = _reader.GetAttribute("ID"),
						Name = _reader.GetAttribute("Наименование")
					};
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
							Code = _reader.GetAttribute("КодТовара"),
							CodeCr = _reader.GetAttribute("КодПроизводителя"),
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

	public class FarmaimpeksFormalizer : IPriceFormalizer
	{
		private string _fileName;
		private DataTable _data;
		private PriceFormalizationInfo _priceInfo;
		private ILog _log = LogManager.GetLogger(typeof (FarmaimpeksFormalizer));

		public FarmaimpeksFormalizer(string filename, MySqlConnection connection, DataTable data)
		{
			_fileName = filename;
			_data = data;
			_priceInfo = new PriceFormalizationInfo(data.Rows[0]);
		}

		public IList<string> GetAllNames()
		{
			List<string> names = new List<string>();          
			using (PriceXmlReader reader = new PriceXmlReader(_fileName))
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

					IList<string> ls = parser.GetAllNames();
					if (ls != null)
						names.AddRange(ls);
				}
			}            
			return names;
		}

		public void Formalize()
		{
			var reader = new PriceXmlReader(_fileName);
			foreach (var parsedPrice in reader.Prices())
			{
				var priceInfo = _data.Rows[0];
				var supplierId = Convert.ToUInt32(priceInfo["FirmCode"]);
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

				FormalizePrice(reader, priceInfo, cost);

				var customers = reader.Customers().ToList();
				With.Transaction((c, t) => {

					var command = new MySqlCommand(@"
update Usersettings.Pricesdata
set PriceName = ?name
where pricecode = ?PriceId", c);
					command.Parameters.AddWithValue("?PriceId", cost.Price.Id);
					command.Parameters.AddWithValue("?Name", parsedPrice.Name);
					command.ExecuteNonQuery();

					command = new MySqlCommand(@"
update Future.Intersection i
set i.AvailableForClient = 0
where i.PriceId = ?priceId", c);
					command.Parameters.AddWithValue("?priceId", cost.Price.Id);
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
						command.Parameters.AddWithValue("?priceId", cost.Price.Id);
						command.ExecuteNonQuery();
					}
				});
			}
		}

		private void FormalizePrice(PriceXmlReader reader, DataRow priceInfo, PriceCost cost)
		{
			var info = new PriceFormalizationInfo(priceInfo);

			info.IsUpdating = true;
			info.CostCode = cost.Id;
			info.PriceItemId = cost.PriceItemId;
			info.PriceCode = cost.Price.Id;

			var parser = new BasePriceParser2(reader, info);
			parser.Downloaded = Downloaded;
			parser.Formalize();
			formCount += parser.Stat.formCount;
			forbCount += parser.Stat.forbCount;
			unformCount += parser.Stat.unformCount;
			zeroCount += parser.Stat.zeroCount;
		}

		public bool Downloaded { get; set; }
		public string InputFileName { get; set; }

		public int formCount { get; private set; }
		public int unformCount { get; private set; }
		public int zeroCount { get; private set; }
		public int forbCount { get; private set; }

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