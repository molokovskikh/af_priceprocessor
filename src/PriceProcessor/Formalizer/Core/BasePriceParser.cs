using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Common.MySql;
using Common.Tools;
using Dapper;
using Inforoom.PriceProcessor.Formalizer.Helpers;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using MySql.Data.MySqlClient;
using SqlBuilder = Inforoom.PriceProcessor.Formalizer.Helpers.SqlBuilder;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class BasePriceParser
	{
		public static Regex SpaceReg = new Regex(@"\s", RegexOptions.Compiled);

		//Соедиение с базой данных
		protected MySqlConnection _connection;

		//Таблица со списком запрещенных названий
		protected MySqlDataAdapter daForbidden;
		protected DataTable dtForbidden;

		//Таблица со списоком синонимов производителей
		protected MySqlDataAdapter daSynonym;
		protected DataTable dtSynonym;

		//Таблица со списоком синонимов производителей
		protected MySqlDataAdapter daSynonymFirmCr;
		protected DataTable dtSynonymFirmCr;

		//Таблица с исключениями
		protected MySqlDataAdapter daExcludes;
		protected DataTable dtExcludes;

		protected MySqlDataAdapter daUnrecExp;
		protected DataTable dtUnrecExp;
		protected MySqlDataAdapter daZero;
		protected DataTable dtZero;
		protected MySqlDataAdapter daForb;
		protected DataTable dtForb;

		protected DataSet dsMyDB;

		private FormalizeStats _stats = new FormalizeStats();

		//Является ли обрабатываемый прайс-лист загруженным?
		public bool Downloaded;

		//ключ для priceitems
		public long priceItemId;

		//родительский синоним : прайс-родитель, нужен для выбора различных параметров
		protected long parentSynonym;

		//Тип ценовых колонок прайса-родителя: 0 - мультиколоночный, 1 - многофайловый
		protected CostTypes costType;

		protected readonly ILog _logger;

		protected PriceFormalizationInfo _priceInfo;
		protected FormLog _loggingStat;

		private Searcher _searcher;

		private readonly List<NewOffer> _newCores = new List<NewOffer>();
		private readonly List<ExistsOffer> _existsCores = new List<ExistsOffer>();

		private readonly IReader _reader;

		public PriceFormalizationInfo PriceInfo => _priceInfo;

		public FormLog Stat => _loggingStat;

		private ProducerResolver _producerResolver;
		private bool _saveInCore;
		private RejectUpdater _rejectUpdater = new RejectUpdater();
		private DataTable barcodes;

		public BasePriceParser(IReader reader, PriceFormalizationInfo priceInfo, bool saveInCore = false)
		{
			_logger = LogManager.GetLogger(GetType());
			_reader = reader;

			_priceInfo = priceInfo;
			_loggingStat = new FormLog(priceInfo);

			_connection = new MySqlConnection(ConnectionHelper.GetConnectionString());
			dsMyDB = new DataSet();

			priceItemId = _priceInfo.PriceItemId;
			parentSynonym = _priceInfo.ParentSynonym;
			costType = _priceInfo.CostType;

			string selectCostFormRulesSQL;
			if (costType == CostTypes.MultiColumn)
				selectCostFormRulesSQL = String.Format("select *, (exists(select * from usersettings.pricesregionaldata prd where prd.pricecode=pc.pricecode and prd.basecost=pc.costcode limit 1)) as NewBaseCost from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode", _priceInfo.PriceCode);
			else
				selectCostFormRulesSQL = String.Format("select *, (exists(select * from usersettings.pricesregionaldata prd where prd.pricecode=pc.pricecode and prd.basecost=pc.costcode limit 1)) as NewBaseCost from usersettings.PricesCosts pc, farm.CostFormRules cfr where pc.PriceCode={0} and cfr.CostCode = pc.CostCode and pc.CostCode = {1}", _priceInfo.PriceCode, _priceInfo.CostCode.Value);
			var daPricesCost = new MySqlDataAdapter(selectCostFormRulesSQL, _connection);
			var dtPricesCost = new DataTable("PricesCosts");
			daPricesCost.Fill(dtPricesCost);
			_reader.CostDescriptions = dtPricesCost.Rows.Cast<DataRow>().Select(r => new CostDescription(r)).ToList();
			_logger.DebugFormat("Загрузили цены {0}.{1}", _priceInfo.PriceCode, _priceInfo.CostCode);
			_saveInCore = saveInCore;
		}

		/// <summary>
		/// Вставка в таблицу запрещенных предложений
		/// </summary>
		public void InsertIntoForb(FormalizationPosition position)
		{
			var forb = dtForb.Rows.Cast<DataRow>().FirstOrDefault(r => r["Forb"].ToString().Equals(position.PositionName, StringComparison.CurrentCultureIgnoreCase));
			if (forb != null)
				return;
			var newRow = dtForb.NewRow();
			newRow["PriceItemId"] = priceItemId;
			newRow["Forb"] = position.PositionName;
			dtForb.Rows.Add(newRow);
			_loggingStat.Forb++;
		}

		/// <summary>
		/// Вставка записи в Zero
		/// </summary>
		public void InsertToZero(FormalizationPosition position)
		{
			var drZero = dtZero.NewRow();
			var core = position.Offer;

			drZero["PriceItemId"] = priceItemId;
			drZero["Name"] = position.PositionName;
			drZero["FirmCr"] = position.FirmCr;
			drZero["Code"] = core.Code;
			drZero["CodeCr"] = core.CodeCr;
			drZero["Unit"] = core.Unit;
			drZero["Volume"] = core.Volume;
			drZero["Quantity"] = core.Quantity;
			drZero["Note"] = core.Note;
			drZero["Period"] = core.Period;
			drZero["Doc"] = core.Doc;

			dtZero.Rows.Add(drZero);
			_loggingStat.Zero++;
		}

		/// <summary>
		/// Вставка в нераспознанные позиции
		/// </summary>
		public void InsertToUnrec(FormalizationPosition position)
		{
			DataRow drUnrecExp = dtUnrecExp.NewRow();
			drUnrecExp["PriceItemId"] = priceItemId;
			drUnrecExp["Name1"] = position.PositionName;
			drUnrecExp["FirmCr"] = position.FirmCr;
			drUnrecExp["Code"] = position.Offer.Code;
			drUnrecExp["CodeCr"] = position.Offer.CodeCr;
			drUnrecExp["Unit"] = position.Offer.Unit;
			drUnrecExp["Volume"] = position.Offer.Volume;
			drUnrecExp["Quantity"] = position.Offer.Quantity;
			drUnrecExp["Note"] = position.Offer.Note;
			drUnrecExp["Period"] = position.Offer.Period;
			drUnrecExp["Doc"] = position.Offer.Doc;
			if (position.Offer.EAN13 > 0)
				drUnrecExp["EAN13"] = position.Offer.EAN13;

			drUnrecExp["Junk"] = Convert.ToByte(position.Offer.Junk);

			drUnrecExp["AddDate"] = DateTime.Now;

			drUnrecExp["Status"] = (byte)position.Status;
			drUnrecExp["Already"] = (byte)position.Status;

			if (position.ProductId.HasValue)
				drUnrecExp["PriorProductId"] = position.ProductId;
			if (position.CodeFirmCr.HasValue)
				drUnrecExp["PriorProducerId"] = position.CodeFirmCr;
			if (position.SynonymCode.HasValue)
				drUnrecExp["ProductSynonymId"] = position.SynonymCode;
			if (position.SynonymFirmCrCode.HasValue)
				drUnrecExp["ProducerSynonymId"] = position.SynonymFirmCrCode;
			if (position.InternalProducerSynonymId.HasValue)
				drUnrecExp["InternalProducerSynonymId"] = position.InternalProducerSynonymId;

			if (dtUnrecExp.Columns.Contains("HandMade"))
				drUnrecExp["HandMade"] = 0;
			drUnrecExp["ManualDel"] = false;

			dtUnrecExp.Rows.Add(drUnrecExp);
		}

		/// <summary>
		/// Подготовка к разбору прайса, чтение таблиц
		/// </summary>
		public void Prepare()
		{
			_logger.Debug("начало Prepare");
			daForbidden = new MySqlDataAdapter(
				String.Format("SELECT PriceCode, LOWER(Forbidden) AS Forbidden FROM farm.Forbidden WHERE PriceCode={0}", _priceInfo.PriceCode), _connection);
			daForbidden.Fill(dsMyDB, "Forbidden");
			dtForbidden = dsMyDB.Tables["Forbidden"];
			_logger.Debug("загрузили Forbidden");

			daSynonym = new MySqlDataAdapter(String.Format(@"SELECT
	s.SynonymCode,
	LOWER(s.Synonym) AS Synonym,
	s.ProductId,
	s.Junk,
	p.CatalogId,
	c.Pharmacie,
	lower(s.Canonical) as Canonical
FROM farm.Synonym s
	join catalogs.products p on p.Id = s.ProductId
		join Catalogs.Catalog c on c.Id = p.CatalogId
WHERE s.PriceCode = {0}",
				parentSynonym), _connection);
			daSynonym.Fill(dsMyDB, "Synonym");
			dtSynonym = dsMyDB.Tables["Synonym"];
			_logger.Debug("загрузили Synonym");

			daExcludes = new MySqlDataAdapter(
				String.Format("SELECT Id, CatalogId, ProducerSynonym, PriceCode, OriginalSynonymId FROM farm.Excludes where PriceCode = {0}", parentSynonym), _connection);
			var cbExcludes = new MySqlCommandBuilder(daExcludes);
			daExcludes.InsertCommand = cbExcludes.GetInsertCommand();
			daExcludes.InsertCommand.CommandTimeout = 0;
			daExcludes.Fill(dsMyDB, "Excludes");
			dtExcludes = dsMyDB.Tables["Excludes"];
			_logger.Debug("загрузили Excludes");
			dtExcludes.Constraints.Add("ProducerSynonymKey", new[] { dtExcludes.Columns["CatalogId"], dtExcludes.Columns["ProducerSynonym"] }, false);
			_logger.Debug("построили индекс по Excludes");

			daSynonymFirmCr = new MySqlDataAdapter(
				String.Format(@"
SELECT
  SynonymFirmCrCode,
  CodeFirmCr,
  LOWER(Synonym) AS Synonym,
  (aps.ProducerSynonymId is not null) as IsAutomatic,
	Canonical
FROM
  farm.SynonymFirmCr
  left join farm.AutomaticProducerSynonyms aps on aps.ProducerSynonymId = SynonymFirmCr.SynonymFirmCrCode
WHERE SynonymFirmCr.PriceCode={0} and Canonical is not null
",
					parentSynonym),
				_connection);
			daSynonymFirmCr.Fill(dsMyDB, "SynonymFirmCr");
			daSynonymFirmCr.InsertCommand = new MySqlCommand(@"SELECT farm.CreateProducerSynonym(?PriceCode, ?CodeFirmCr, ?OriginalSynonym, ?IsAutomatic);");
			daSynonymFirmCr.InsertCommand.Parameters.Add("?PriceCode", MySqlDbType.Int64);
			daSynonymFirmCr.InsertCommand.Parameters.Add("?OriginalSynonym", MySqlDbType.String);
			daSynonymFirmCr.InsertCommand.Parameters.Add("?CodeFirmCr", MySqlDbType.Int64);
			daSynonymFirmCr.InsertCommand.Parameters.Add("?IsAutomatic", MySqlDbType.Bit);
			daSynonymFirmCr.InsertCommand.Connection = _connection;
			dtSynonymFirmCr = dsMyDB.Tables["SynonymFirmCr"];
			dtSynonymFirmCr.Columns.Add("OriginalSynonym", typeof(string));
			dtSynonymFirmCr.Columns.Add("InternalProducerSynonymId", typeof(long));
			dtSynonymFirmCr.Columns["InternalProducerSynonymId"].AutoIncrement = true;
			_logger.Debug("загрузили SynonymFirmCr");

			var adapter = new MySqlDataAdapter(@"select
b.ProductId, p.CatalogId, b.ProducerId, b.EAN13, c.Pharmacie
from Catalogs.BarcodeProducts b
	join Catalogs.Products p on b.ProductId = p.Id
		join Catalogs.Catalog c on c.Id = p.CatalogId", _connection);
			barcodes = new DataTable();
			adapter.Fill(barcodes);

			_producerResolver = new ProducerResolver(_stats, dtExcludes, dtSynonymFirmCr);
			_producerResolver.Load(_connection);

			daUnrecExp = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.UnrecExp WHERE PriceItemId={0} LIMIT 0", priceItemId), _connection);
			var cbUnrecExp = new MySqlCommandBuilder(daUnrecExp);
			daUnrecExp.AcceptChangesDuringUpdate = false;
			daUnrecExp.InsertCommand = cbUnrecExp.GetInsertCommand();
			daUnrecExp.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
			daUnrecExp.InsertCommand.CommandTimeout = 0;
			daUnrecExp.Fill(dsMyDB, "UnrecExp");
			dtUnrecExp = dsMyDB.Tables["UnrecExp"];
			dtUnrecExp.Columns["AddDate"].DataType = typeof(DateTime);
			dtUnrecExp.Columns.Add("InternalProducerSynonymId", typeof(long));
			_logger.Debug("загрузили UnrecExp");

			daZero = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Zero WHERE PriceItemId={0} LIMIT 0", priceItemId), _connection);
			var cbZero = new MySqlCommandBuilder(daZero);
			daZero.InsertCommand = cbZero.GetInsertCommand();
			daZero.InsertCommand.CommandTimeout = 0;
			daZero.Fill(dsMyDB, "Zero");
			dtZero = dsMyDB.Tables["Zero"];
			_logger.Debug("загрузили Zero");

			daForb = new MySqlDataAdapter(
				String.Format("SELECT * FROM farm.Forb WHERE PriceItemId={0} LIMIT 0", priceItemId), _connection);
			var cbForb = new MySqlCommandBuilder(daForb);
			daForb.InsertCommand = cbForb.GetInsertCommand();
			daForb.InsertCommand.CommandTimeout = 0;
			daForb.Fill(dsMyDB, "Forb");
			dtForb = dsMyDB.Tables["Forb"];
			dtForb.Constraints.Add("ForbName", new[] { dtForb.Columns["Forb"] }, false);
			_logger.Debug("загрузили Forb");

			if (_priceInfo.IsUpdating) {
				var loadExistsWatch = Stopwatch.StartNew();
				LoadCore();
				_logger.Debug("Загрузили предложения");
				if (_existsCores.Count > 0) {
					LoadCosts();
					_logger.Debug("Загрузили цены");
				}
				if(_saveInCore)
					_searcher = new Searcher(_existsCores, new[] { typeof(Offer).GetField("CodeOKP") });
				else
					_searcher = new Searcher(_existsCores);
				loadExistsWatch.Stop();
				_logger.InfoFormat("Загрузка и подготовка существующего прайса, {0}с", loadExistsWatch.Elapsed.TotalSeconds);
			}

			_logger.Debug("конец Prepare");
		}

		private void LoadCore()
		{
			string existsCoreSQL;
			if (costType == CostTypes.MultiColumn)
				existsCoreSQL = String.Format("SELECT Core0.* FROM farm.Core0 WHERE PriceCode={0} order by Id", _priceInfo.PriceCode);
			else
				existsCoreSQL = String.Format("SELECT Core0.* FROM farm.Core0, farm.CoreCosts WHERE Core0.PriceCode={0} and CoreCosts.Core_Id = Core0.id and CoreCosts.PC_CostCode = {1} order by Core0.Id", _priceInfo.PriceCode, _priceInfo.CostCode);

			var command = new MySqlCommand(existsCoreSQL, _connection);
			using (var reader = command.ExecuteReader()) {
				while (reader.Read()) {
					var existsCore = new ExistsOffer {
						Id = reader.GetUInt64("Id")
					};
					foreach (var map in Mapping.OfferMapping) {
						map.SetValue(GetReaderValue(reader, reader.GetOrdinal(map.Name), map.Type), existsCore);
					}
					_existsCores.Add(existsCore);
				}
			}
		}

		private void LoadCosts()
		{
			string existsCoreCostsSQL;
			if (costType == CostTypes.MultiColumn)
				existsCoreCostsSQL = String.Format(@"
select cc.*
from farm.Core0 c
	join farm.CoreCosts cc on cc.Core_Id = c.id
where c.PriceCode = {0}
order by c.Id",
					_priceInfo.PriceCode);
			else
				existsCoreCostsSQL = String.Format("SELECT CoreCosts.* FROM farm.Core0, farm.CoreCosts WHERE Core0.PriceCode={0} and CoreCosts.Core_Id = Core0.id and CoreCosts.PC_CostCode = {1} order by Core0.Id", _priceInfo.PriceCode, _priceInfo.CostCode);
			var costsCommand = new MySqlCommand(existsCoreCostsSQL, _connection);
			using (var reader = costsCommand.ExecuteReader()) {
				var index = 0;
				var core = _existsCores[0];
				var costs = new List<Cost>();
				while (reader.Read()) {
					var coreId = reader.GetUInt64(0);
					var costId = reader.GetUInt32(1);
					var description = _reader.CostDescriptions.First(c => c.Id == costId);
					if (coreId != core.Id) {
						core.Costs = costs.ToArray();
						costs = new List<Cost>();
						core = null;
						for (var i = index; i < _existsCores.Count; i++) {
							if (_existsCores[i].Id == coreId) {
								index = i;
								core = _existsCores[i];
								break;
							}
						}
						if (core == null)
							throw new Exception(String.Format("Не удалось найти позицию в Core, Id = {0}", coreId));
					}
					costs.Add(new Cost(description, reader.GetDecimal("Cost")) {
						RequestRatio = GetUintOrDbNUll(reader, reader.GetOrdinal("RequestRatio")),
						MinOrderSum = GetDecimalOrDbNull(reader, reader.GetOrdinal("MinOrderSum")),
						MinOrderCount = GetUintOrDbNUll(reader, reader.GetOrdinal("MinOrderCount")),
					});
				}
				core.Costs = costs.ToArray();
			}
			_logger.Debug("Загрузили цены");
		}

		public object GetReaderValue(MySqlDataReader reader, int index, Type type)
		{
			if (type == typeof(uint))
				return GetUintOrDbNUll(reader, index);
			if (type == typeof(ulong))
				return GetUlongOrDbNUll(reader, index);
			if (type == typeof(decimal))
				return GetDecimalOrDbNull(reader, index);
			if (type == typeof(string))
				return reader.IsDBNull(index) ? "" : reader.GetString(index);
			if (type == typeof(bool))
				return reader.GetBoolean(index);
			if (type == typeof(DateTime))
				return reader.IsDBNull(index) ? DateTime.MinValue : reader.GetDateTime(index);

			throw new Exception(String.Format("Не знаю как считать тип {0}", type));
		}

		public uint GetUintOrDbNUll(MySqlDataReader reader, int index)
		{
			if (reader.IsDBNull(index))
				return 0;
			return reader.GetUInt32(index);
		}

		public ulong GetUlongOrDbNUll(MySqlDataReader reader, int index)
		{
			if (reader.IsDBNull(index))
				return 0ul;
			return reader.GetUInt64(index);
		}

		public decimal GetDecimalOrDbNull(MySqlDataReader reader, int index)
		{
			if (reader.IsDBNull(index))
				return 0;
			return reader.GetDecimal(index);
		}

		public string StatCommand(MySqlCommand command)
		{
			var startTime = DateTime.UtcNow;
			var applyCount = command.ExecuteNonQuery();
			var workTime = DateTime.UtcNow.Subtract(startTime);
			return String.Format("{0};{1}", applyCount, workTime);
		}

		public string TryUpdate(MySqlDataAdapter da, DataTable dt, MySqlTransaction tran)
		{
			var startTime = DateTime.UtcNow;
			da.SelectCommand.Transaction = tran;
			var applyCount = da.Update(dt);
			var workTime = DateTime.UtcNow.Subtract(startTime);
			return String.Format("{0};{1}", applyCount, workTime);
		}

		private IEnumerable<string> BuildSql()
		{
			foreach (var core in _newCores) {
				if (core.ExistsOffer == null) {
					Stat.InsertCoreCount++;
					yield return SqlBuilder.InsertOfferSql(_priceInfo, core);
					yield return SqlBuilder.InsertCostSql(core, Stat);
				}
				else {
					yield return SqlBuilder.UpdateOfferSql(core, Stat);
					yield return SqlBuilder.UpdateCostsCommand(core, Stat);
				}
			}

			var forDelete = _existsCores.Where(c => c.NewOffer == null).Select(c => c.Id.ToString()).ToArray();
			if (forDelete.Length > 0) {
				Stat.DeleteCoreCount += forDelete.Length;
				yield return "delete from farm.Core0 where Core0.Id in (" + String.Join(", ", forDelete.ToArray()) + ");";
			}
		}

		public IDisposable Profile(string text)
		{
			var watch = Stopwatch.StartNew();
			return new DisposibleAction(() => {
				watch.Stop();
				_logger.DebugFormat("{0}, {1}с", text, watch.Elapsed.TotalSeconds);
			});
		}

		/// <summary>
		/// Окончание разбора прайса, с последующим логированием статистики
		/// </summary>
		public void FinalizePrice()
		{
			//Проверку и отправку уведомлений производим только для загруженных прайс-листов
			if (Downloaded) {
				_reader.SendWarning(_loggingStat);
				SendWarning(_loggingStat);
			}

			if (Settings.Default.CheckZero && (_loggingStat.Zero.GetValueOrDefault() > (_loggingStat.Form.GetValueOrDefault() + _loggingStat.UnForm.GetValueOrDefault() + _loggingStat.Zero.GetValueOrDefault()) * 0.95))
				throw new RollbackFormalizeException(Settings.Default.ZeroRollbackError, _priceInfo, _loggingStat);

			if (_loggingStat.Form.GetValueOrDefault() * 4 < _priceInfo.PrevRowCount)
				throw new RollbackFormalizeException(Settings.Default.PrevFormRollbackError, _priceInfo, _loggingStat);

			var transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);

			try {
				CleanupCore();

				using (Profile("Вставка синонимов товаров"))
					InsertProductSynonyms(transaction);

				using (Profile("Вставка синонимов производителей"))
					InsertProducerSynonyms(transaction);

				using (Profile("Создание ценовых колонок"))
					InsertNewCosts();

				var builder = new StringBuilder();
				var command = new MySqlCommand(null, _connection);
				foreach (var populatedBytes in PrepareData((c, l) => builder.Append(c))) {
					command.CommandText = builder.ToString();
#if DEBUG
					if (_logger.IsDebugEnabled)
						_logger.Debug(command.CommandText);
#endif
					builder.Clear();
					using (Profile("Обновление Core и CoreCosts"))
						command.ExecuteNonQuery();
				}
				Maintain(transaction);

				transaction.Commit();
			}
			catch (Exception) {
				With.SafeRollback(transaction);
				throw;
			}
		}

		private void CleanupCore()
		{
			if (PriceInfo.IsUpdating)
				return;

			var command = new MySqlCommand("", _connection);
			if (costType == CostTypes.MiltiFile) {
				command.CommandText = String.Format(@"
delete
farm.Core0
from
farm.CoreCosts,
farm.Core0
where
CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1};",
					PriceInfo.PriceCode, PriceInfo.CostCode);
			}
			else {
				command.CommandText = String.Format("delete from farm.Core0 where PriceCode={0};", PriceInfo.PriceCode);
			}

			_logger.InfoFormat("Удаление записей из Core0 {0}", StatCommand(command));
		}

		private IEnumerable<int> PrepareData(Action<string, int> populateCommand)
		{
			var populatedBytes = 0;
			foreach (var command in BuildSql().Where(c => !String.IsNullOrEmpty(c))) {
				if (populatedBytes + command.Length > Settings.Default.MySqlMaxPacketSize) {
					yield return populatedBytes;
					populatedBytes = 0;
				}
				populateCommand(command, populatedBytes);
				populatedBytes += command.Length;
			}
			if (populatedBytes > 0)
				yield return populatedBytes;
		}

		private void Maintain(MySqlTransaction transaction)
		{
			var cleanUpCommand = new MySqlCommand {
				Connection = _connection,
				CommandTimeout = 0
			};
			cleanUpCommand.CommandText = String.Format("delete from farm.Zero where PriceItemId={0}", priceItemId);
			_logger.DebugFormat("DelFromZero={0}  ", StatCommand(cleanUpCommand));

			cleanUpCommand.CommandText = String.Format("delete from farm.Forb where PriceItemId={0}", priceItemId);
			_logger.DebugFormat("DelFromForb={0}  ", StatCommand(cleanUpCommand));

			var daBlockedPrice = new MySqlDataAdapter(String.Format("SELECT * FROM farm.blockedprice where PriceItemId={0} limit 1", priceItemId), _connection);
			daBlockedPrice.SelectCommand.Transaction = transaction;
			var dtBlockedPrice = new DataTable();
			daBlockedPrice.Fill(dtBlockedPrice);

			if (dtBlockedPrice.Rows.Count == 0) {
				cleanUpCommand.CommandText = String.Format("delete from farm.UnrecExp where PriceItemId={0} and ManualDel=0", priceItemId);
				_logger.DebugFormat("DelFromUnrecExp={0}  ", StatCommand(cleanUpCommand));
			}

			_logger.DebugFormat("UpdateForb={0}  ", TryUpdate(daForb, dtForb.Copy(), transaction));
			_logger.DebugFormat("UpdateZero={0}  ", TryUpdate(daZero, dtZero.Copy(), transaction));
			_logger.DebugFormat("UpdateUnrecExp={0}  ", UnrecExpUpdate(transaction));
			//Исключения обновляем после нераспознанных, т.к. все может измениться
			_logger.DebugFormat("UpdateExcludes={0}  ", TryUpdate(daExcludes, dtExcludes.Copy(), transaction));

			//Производим обновление PriceDate и LastFormalization в информации о формализации
			//Если прайс-лист загружен, то обновляем поле PriceDate, если нет, то обновляем данные в intersection_update_info
			cleanUpCommand.Parameters.Clear();
			if (Downloaded) {
				cleanUpCommand.CommandText = String.Format(
					"UPDATE usersettings.PriceItems SET RowCount={0}, PriceDate=now(), LastFormalization=now(), UnformCount={1} WHERE Id={2};",
						_loggingStat.Form.GetValueOrDefault(), _loggingStat.UnForm.GetValueOrDefault(), priceItemId);
			}
			else {
				cleanUpCommand.CommandText = String.Format(
					"UPDATE usersettings.PriceItems SET RowCount={0}, LastFormalization=now(), UnformCount={1} WHERE Id={2};",
						_loggingStat.Form.GetValueOrDefault(), _loggingStat.UnForm.GetValueOrDefault(), priceItemId);
			}
			cleanUpCommand.CommandText += String.Format(@"
UPDATE usersettings.AnalitFReplicationInfo A, usersettings.PricesData P
SET
  a.ForceReplication = 1
where
	p.PriceCode = {0}
and a.FirmCode = p.FirmCode;",
				_priceInfo.PriceCode);

			_logger.DebugFormat("UpdatePriceItemsAndIntersections={0}  ", StatCommand(cleanUpCommand));

			_stats.PrintSearchStats();
		}

		private string UnrecExpUpdate(MySqlTransaction finalizeTransaction)
		{
			return UnrecExpUpdate(finalizeTransaction, dtUnrecExp, dtSynonymFirmCr, daUnrecExp);
		}

		public static string UnrecExpUpdate(MySqlTransaction finalizeTransaction, DataTable unrecExpressions, DataTable producerSynonyms, MySqlDataAdapter adapter)
		{
			var startTime = DateTime.UtcNow;
			TimeSpan workTime;
			var applyCount = 0;

			adapter.SelectCommand.Transaction = finalizeTransaction;
			try {
				foreach (var drUnrecExp in unrecExpressions.AsEnumerable()) {
					if (!Convert.IsDBNull(drUnrecExp["InternalProducerSynonymId"])) {
						var drsProducerSynonyms = producerSynonyms.Select(String.Format("InternalProducerSynonymId = {0}", drUnrecExp["InternalProducerSynonymId"]));

						if ((drsProducerSynonyms.Length == 0) && !Convert.IsDBNull(drUnrecExp["InternalProducerSynonymId"]))
							throw new Exception(String.Format("Не нашли новых синонимов хотя ссылка существует: {0}  {1}", drUnrecExp["FirmCr"], drUnrecExp));
						if (drsProducerSynonyms.Length > 1)
							throw new Exception(String.Format("Получили новых синонимов больше 1: {0} {1}", drUnrecExp["FirmCr"], drUnrecExp));

						drUnrecExp["ProducerSynonymId"] = drsProducerSynonyms[0]["SynonymFirmCrCode"];
						//Если найденный синоним новый и был обновлен при сохранении прайс-листа в базу
						//и если не сбрасывали ссылку на новый синоним
						if ((drsProducerSynonyms[0].RowState == DataRowState.Unchanged) && !Convert.IsDBNull(drUnrecExp["InternalProducerSynonymId"])) {
							drUnrecExp["InternalProducerSynonymId"] = DBNull.Value;
							//Если синоним не автоматический, то будем выставлять CodeFirmCr
							if (!Convert.ToBoolean(drsProducerSynonyms[0]["IsAutomatic"])) {
								//Если CodeFirmCr не установлен, то синоним производителя сопоставлен с "производитель не известен"
								if (Convert.IsDBNull(drsProducerSynonyms[0]["CodeFirmCr"])) {
									if (!Convert.IsDBNull(drUnrecExp["PriorProductId"])) {
										//Если сопоставлено по наименованию, то она полностью сопоставлена и удаляем из нераспознанных
										drUnrecExp["Already"] = (byte)UnrecExpStatus.FullForm;
										drUnrecExp["Status"] = (byte)UnrecExpStatus.FullForm;
										continue;
									}
									drUnrecExp["Already"] = (byte)UnrecExpStatus.FirmForm;
									drUnrecExp["Status"] = (byte)UnrecExpStatus.FirmForm;
								}
								else if (Convert.IsDBNull(drUnrecExp["PriorProductId"])) {
									drUnrecExp["PriorProducerId"] = drsProducerSynonyms[0]["CodeFirmCr"];
									drUnrecExp["Already"] = (byte)((UnrecExpStatus)((byte)drUnrecExp["Already"]) | UnrecExpStatus.FirmForm);
									drUnrecExp["Status"] = (byte)((UnrecExpStatus)((byte)drUnrecExp["Status"]) | UnrecExpStatus.FirmForm);
								}
								else {
									drUnrecExp["PriorProducerId"] = drsProducerSynonyms[0]["CodeFirmCr"];
									drUnrecExp["Already"] = (byte)(UnrecExpStatus.NameForm | UnrecExpStatus.FirmForm);
									drUnrecExp["Status"] = (byte)(UnrecExpStatus.NameForm | UnrecExpStatus.FirmForm);
									continue;
								}
							}
						}
					}

					//Если не получилось, что позиция из-за вновь созданных синонимов была полностью распознана, то обновляем ее в базе
					if ((((UnrecExpStatus)((byte)drUnrecExp["Status"]) & UnrecExpStatus.FullForm) != UnrecExpStatus.FullForm)) {
						drUnrecExp["ManualDel"] = false;
						adapter.Update(new[] { drUnrecExp });
						applyCount++;
					}
				}
			}
			finally {
				workTime = DateTime.UtcNow.Subtract(startTime);
			}
			return String.Format("{0};{1}", applyCount, workTime);
		}

		private void InsertProductSynonyms(MySqlTransaction finalizeTransaction)
		{
				var sql = @"insert into Farm.Synonym(PriceCode, Synonym, ProductId) values (?priceCode, ?synonym, ?productId);
select last_insert_id();";
			var cmd = new MySqlCommand(sql, _connection);
			cmd.Transaction = finalizeTransaction;
			foreach (var row in dtSynonym.AsEnumerable().Where(x => x["SynonymCode"] is DBNull)) {
				Stat.InsertProductSynonymCount++;
				//var parameters = new { priceCode = parentSynonym, synonym = row["Synonym"],
				//	productId = row["ProductId"],
				//	childPriceCode = PriceInfo.Price.Id,
				//};
				cmd.Parameters.Clear();
				cmd.Parameters.AddWithValue("?priceCode", parentSynonym);
				cmd.Parameters.AddWithValue("?synonym", row["Synonym"]);
				cmd.Parameters.AddWithValue("?productId", row["ProductId"]);
				//cmd.Parameters.AddWithValue("?childPriceCode", PriceInfo.Price.Id);
				row["SynonymCode"] = Convert.ToUInt64(cmd.ExecuteScalar());
			}
			foreach (var core in _newCores.Where(c => c.CreatedProductSynonym != null))
				core.SynonymCode = Convert.ToUInt32(core.CreatedProductSynonym["SynonymCode"]);
			if (Stat.InsertProductSynonymCount > 0)
				_logger.Info($"Создано синонимов производителей {Stat.InsertProductSynonymCount}");
		}

		private void InsertProducerSynonyms(MySqlTransaction finalizeTransaction)
		{
			if (!_stats.CanCreateProducerSynonyms())
				return;
			daSynonymFirmCr.InsertCommand.Connection = _connection;
			daSynonymFirmCr.InsertCommand.Transaction = finalizeTransaction;
			foreach (var drNewProducerSynonym in dtSynonymFirmCr.Select("SynonymFirmCrCode is null")) {
				Stat.InsertProducerSynonymCount++;
				daSynonymFirmCr.InsertCommand.Parameters["?PriceCode"].Value = parentSynonym;
				daSynonymFirmCr.InsertCommand.Parameters["?OriginalSynonym"].Value = drNewProducerSynonym["OriginalSynonym"];
				daSynonymFirmCr.InsertCommand.Parameters["?CodeFirmCr"].Value = drNewProducerSynonym["CodeFirmCr"];
				daSynonymFirmCr.InsertCommand.Parameters["?IsAutomatic"].Value = drNewProducerSynonym["IsAutomatic"];
				drNewProducerSynonym["SynonymFirmCrCode"] = Convert.ToInt64(daSynonymFirmCr.InsertCommand.ExecuteScalar());
			}

			foreach (var core in _newCores.Where(c => c.CreatedProducerSynonym != null))
				core.SynonymFirmCrCode = Convert.ToUInt32(core.CreatedProducerSynonym["SynonymFirmCrCode"]);
			if (Stat.InsertProducerSynonymCount > 0)
				_logger.Info($"Создано синонимов производителей {Stat.InsertProducerSynonymCount}");
		}

		/// <summary>
		/// анализируем цены и формируем список, если ценовая колонка имеет более 85% позиций с неустановленной ценой
		/// </summary>
		private void ProcessUndefinedCost(FormLog stat)
		{
			var stringBuilder = new StringBuilder();
			foreach (var cost in _reader.CostDescriptions)
				if (cost.UndefinedCostCount > stat.Form * 0.85)
					stringBuilder.AppendFormat("ценовая колонка \"{0}\" имеет {1} позиций с незаполненной ценой\n", cost.Name, cost.UndefinedCostCount);

			Alerts.ToManyZeroCostAlert(stringBuilder, _priceInfo);
		}

		/// <summary>
		/// анализируем цены и формируем сообщение, если ценовая колонка имеет все позиции установленными в 0
		/// </summary>
		private void ProcessZeroCost(FormLog stat)
		{
			var stringBuilder = new StringBuilder();
			foreach (var cost in _reader.CostDescriptions)
				if ((cost.ZeroCostCount > 0 && stat.Form.GetValueOrDefault() == 0) || cost.ZeroCostCount == stat.Form.GetValueOrDefault())
					stringBuilder.AppendFormat("ценовая колонка \"{0}\" полностью заполнена '0'\n", cost.Name);

			Alerts.ZeroCostAlert(stringBuilder, _priceInfo);
		}

		private void SendWarning(FormLog stat)
		{
			ProcessZeroCost(stat);
			ProcessUndefinedCost(stat);
		}

		/// <summary>
		/// Формализование прайса
		/// </summary>
		public void Formalize()
		{
			try {
				_connection.Open();

				using (Timer("Загрузка данных"))
					Prepare();

				using (Timer("Формализация"))
					InternalFormalize();

				using (Timer("Применение изменений в базу"))
					FinalizePrice();
			}
			finally {
				_connection.Close();
			}

			new BuyingMatrixProcessor().UpdateBuyingMatrix(PriceInfo.Price);
			if (PriceInfo.Price.PostProcessing.Match("UniqMaxCost")) {
				UniqMaxCost();
			}

			if (PriceInfo.Price.IsRejects || PriceInfo.Price.IsRejectCancellations)
				_rejectUpdater.Save(PriceInfo.Price.IsRejectCancellations);
		}

		private void UniqMaxCost()
		{
			SessionHelper.StartSession(s => {
				var sql = @"
drop temporary table if exists Farm.MaxCosts;
create temporary table Farm.MaxCosts engine = memory
select SynonymCode, SynonymFirmCrCode, Max(Cost) as Cost
from Farm.Core0 c
join Farm.CoreCosts cc on cc.Core_ID = c.Id
where c.PriceCode = :priceId
group by c.SynonymCode, c.SynonymFirmCrCode;

drop temporary table if exists Farm.UniqIds;
create temporary table Farm.UniqIds engine = memory
select min(c.Id) as Id
from Farm.core0 c
join Farm.CoreCosts cc on cc.Core_ID = c.Id
join Farm.MaxCosts m on m.SynonymCode = c.SynonymCode and m.SynonymFirmCrCode = c.SynonymFirmCrCode and m.Cost = cc.Cost
group by c.SynonymCode, c.SynonymFirmCrCode;

delete c from Farm.Core0 c
where not exists(select * from Farm.UniqIds i where c.Id = i.Id)
	and c.PriceCode = :priceId;

update Farm.Core0 c
	join Catalogs.Products p on p.Id = c.ProductId
		join Catalogs.Catalog ca on ca.Id = p.CatalogId
set ca.VitallyImportant = 1
where c.PriceCode = :priceId;

drop temporary table Farm.UniqIds;
drop temporary table Farm.MaxCosts;
";
				s.CreateSQLQuery(sql)
					.SetParameter("priceId", PriceInfo.Price.Id)
					.ExecuteUpdate();
			});
		}

		public IDisposable Timer(string message)
		{
			var watch = Stopwatch.StartNew();
			return new DisposibleAction(() => {
				watch.Stop();
				_logger.InfoFormat("{0}, {1}с", message, watch.Elapsed.TotalSeconds);
			});
		}

		private void InternalFormalize()
		{
			foreach (var position in _reader.Read()) {
				position.CalculateJunk();

				if (IsForbidden(position.PositionName)) {
					InsertIntoForb(position);
					//если это забраковка публиковать нужно даже запрешенные позиции
					PostProcessPosition(position);
					continue;
				}

				var core = position.Offer;
				if (!_priceInfo.IsAssortmentPrice) {
					//Если кол-во ненулевых цен = 0, то тогда производим вставку в Zero
					//или если количество определенно и оно равно 0
					if (core.Costs.Length == 0 || core.QuantityAsInt == 0) {
						InsertToZero(position);
						continue;
					}
				}

				if (position.Offer.EAN13 > 0) {
					var barcode = barcodes.Select($"EAN13 = {position.Offer.EAN13}").FirstOrDefault();
					if (barcode != null) {
						var productSynonyms = LookupProductSynonym(position);
						if (productSynonyms != null) {
							position.UpdateProductSynonym(productSynonyms.FirstOrDefault()
								?? CreateProductSynonym(barcode, position.PositionName ?? position.OriginalName));
							//товары в синониме и штрих коде могут не совпасть, предпочитаем штрих код
							position.ProductId = Convert.ToInt64(barcode["ProductId"]);
							position.CatalogId = Convert.ToInt64(barcode["CatalogId"]);
							position.Pharmacie = Convert.ToBoolean(barcode["Pharmacie"]);
							var producerSynonyms = _producerResolver.LookupProducerSynonym(position);
							if (producerSynonyms != null) {
								//при привязке по штрих коду всегда используем синоним без производителя, что бы не создавать синонимы которые могут быть
								//применены при сопоставлении по наименованию
								//тк поставщики часто пишут в производителе только название страны
								var producerSynonym = producerSynonyms.FirstOrDefault(x => Convert.ToUInt32(x["CodeFirmCr"]) == Convert.ToUInt32(barcode["ProducerId"]))
									?? producerSynonyms.FirstOrDefault(x => x["CodeFirmCr"] is DBNull);
								position.NotCreateUnrecExp = true;
								position.UpdateProducerSynonym(producerSynonym
									?? _producerResolver.CreateProducerSynonym(position, null, count: false));
							}
							position.CodeFirmCr = Convert.ToUInt32(barcode["ProducerId"]);
							position.IsSet(UnrecExpStatus.FirmForm);
						}
					}
				}

				if (!position.IsSet(UnrecExpStatus.NameForm)) {
					GetProductId(position);
					_producerResolver.ResolveProducer(position);
				}

				if (!position.IsSet(UnrecExpStatus.NameForm))
					_loggingStat.UnForm++;
				else
					_loggingStat.Form++;

				if (position.IsNotSet(UnrecExpStatus.FullForm) && !position.NotCreateUnrecExp)
					InsertToUnrec(position);

				if (position.IsSet(UnrecExpStatus.NameForm)) {
					core.ProductId = (uint)position.ProductId.GetValueOrDefault();
					core.SynonymCode = (uint)position.SynonymCode.GetValueOrDefault();
					core.CodeFirmCr = (uint)position.CodeFirmCr.GetValueOrDefault();
					core.SynonymFirmCrCode = (uint)position.SynonymFirmCrCode.GetValueOrDefault();
					if (_priceInfo.IsUpdating)
						core.ExistsOffer = _searcher.Find(core);
					_newCores.Add(core);
				}

				PostProcessPosition(position);
			}
		}

		private void PostProcessPosition(FormalizationPosition position)
		{
			if (PriceInfo.Price.IsRejects || PriceInfo.Price.IsRejectCancellations) {
				_rejectUpdater.Process(position);
			}
		}

		//Содержится ли название в таблице запрещенных слов
		public bool IsForbidden(string PosName)
		{
			if(PosName == null)
				return false;
			var dr = dtForbidden.Select(String.Format("Forbidden = '{0}'", PosName.Replace("'", "''")));
			return dr.Length > 0;
		}

		//Смогли ли мы распознать позицию по коду, имени и оригинальному названию?
		public void GetProductId(FormalizationPosition position)
		{
			DataRow dr = null;
			if (_priceInfo.FormByCode) {
				if (!String.IsNullOrWhiteSpace(position.Code)) {
					var code = position.Code?.Trim().Replace("'", "''");
					dr = dtSynonym.Select($"Code = {code}").FirstOrDefault();
				}
			} else {
				dr = LookupProductSynonym(position)?.FirstOrDefault();
			}

			if (dr != null)
				position.UpdateProductSynonym(dr);
		}

		private DataRow[] LookupProductSynonym(FormalizationPosition position)
		{
			DataRow[] result = null;
			if (!String.IsNullOrWhiteSpace(position.PositionName)) {
				//var canonical = SpaceReg.Replace(position.PositionName, "").ToLower().Replace("'", "''");
				//result = dtSynonym.Select($"Canonical = '{canonical}'");
				//if (result.Length == 0) {
					var name = position.PositionName.ToLower().Replace("'", "''");
					result = dtSynonym.Select($"Synonym = '{name}'");
				//}
			}
			if ((result == null || result.Length == 0) && !String.IsNullOrWhiteSpace(position.OriginalName)) {
				//var originalName = SpaceReg.Replace(position.OriginalName, "").Replace("'", "''");
				//result = dtSynonym.Select($"Canonical = '{originalName}'");
				//if (result.Length == 0) {
					var name = position.OriginalName.ToLower().Replace("'", "''");
					result = dtSynonym.Select($"Synonym = '{name}'");
				//}
			}
			return result;
		}

		private DataRow CreateProductSynonym(DataRow barcode, string name)
		{
			var productSynonym = dtSynonym.NewRow();
			productSynonym["ProductId"] = barcode["ProductId"];
			productSynonym["CatalogId"] = barcode["CatalogId"];
			productSynonym["Synonym"] = name.Trim();
			productSynonym["Pharmacie"] = barcode["Pharmacie"];
			productSynonym["Canonical"] = SpaceReg.Replace(name, "");
			productSynonym["Junk"] = false;
			dtSynonym.Rows.Add(productSynonym);
			return productSynonym;
		}

		public void InsertNewCosts()
		{
			var toCreate = _reader.CostDescriptions.Where(d => d.Id == 0);
			foreach (var description in toCreate) {
				description.Id = CostCollumnCreator.CreateCost(_connection,
					_priceInfo.PriceCode,
					(int)_priceInfo.CostType,
					description.Name,
					true,
					true);
			}
		}
	}
}