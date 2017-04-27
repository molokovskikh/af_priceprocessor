using System;
using System.Data;
using System.Linq;
using Common.MySql;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class ProducerResolver
	{
		private ILog _logger = LogManager.GetLogger(typeof(ProducerResolver));

		private FormalizeStats _stats;

		public DataTable Assortment;
		public DataTable MonobrendAssortment;
		public DataTable ForbiddenProdusers;
		private DataTable _excludes;
		private DataTable _producerSynonyms;

		public ProducerResolver(FormalizeStats stats, DataTable excludes, DataTable producerSynonyms)
		{
			_stats = stats;
			_excludes = excludes;
			_producerSynonyms = producerSynonyms;
		}

		/// <summary>
		/// Проверяем, что производитель не занесен в таблицу запрещенных
		/// </summary>
		/// <returns></returns>
		private bool CheckForbiddenProducerName(FormalizationPosition position)
		{
			if(ForbiddenProdusers == null)
				return false;
			var name = ForbiddenProdusers.Select(String.Format("Name = '{0}'", position.FirmCr.ToLower().Replace("'", "''")));
			return name.Length > 0;
		}

		public void ResolveProducer(FormalizationPosition position)
		{
			if (!position.IsSet(UnrecExpStatus.NameForm))
				return;
			//если уже формализован по штрих коду
			if (position.IsSet(UnrecExpStatus.FirmForm))
				return;

			if (String.IsNullOrEmpty(position.FirmCr)) {
				position.AddStatus(UnrecExpStatus.FirmForm);
				return;
			}
			position.NotCreateUnrecExp = CheckForbiddenProducerName(position);
			var synonym = Resolve(position);
			if (synonym == null || synonym["CodeFirmCr"] is DBNull) {
				var producerId = GetAssortimentOne(position)?["ProducerId"];
				if (synonym == null || producerId != null)
					synonym = CreateProducerSynonym(position, producerId);
			}

			position.UpdateProducerSynonym(synonym);
			if (position.SynonymFirmCrCode != null) {
				_stats.ProducerSynonymUsedExistCount++;
			}

			if (position.CodeFirmCr == null && !position.NotCreateUnrecExp)
				CheckExclude(position);
		}

		public DataRow Resolve(FormalizationPosition position)
		{
			var synonyms = LookupProducerSynonym(position);
			if (position.Pharmacie) {
				var assortment = Assortment.Select(String.Format("CatalogId = {0} and Checked = 1", position.CatalogId));
				foreach (var productSynonym in synonyms) {
					if (productSynonym["CodeFirmCr"] is DBNull)
						continue;

					using (_stats.AssortmentSearch()) {
						if (assortment.Any(a => Convert.ToUInt32(a["ProducerId"]) == Convert.ToUInt32(productSynonym["CodeFirmCr"])))
							return productSynonym;
					}
				}
				return synonyms.FirstOrDefault(s => s["CodeFirmCr"] is DBNull);
			} else {
				//предпочитаем синонимы с производителем
				return synonyms.FirstOrDefault(s => !(s["CodeFirmCr"] is DBNull)) ?? synonyms.FirstOrDefault();
			}
		}

		public DataRow[] LookupProducerSynonym(FormalizationPosition position)
		{
			if (string.IsNullOrWhiteSpace(position.FirmCr))
				return null;
			//var canonical = BasePriceParser.SpaceReg.Replace(position.FirmCr, "").ToLower().Replace("'", "''");
			//var result = _producerSynonyms.Select($"Canonical = '{canonical}'");
			//if (result.Length == 0) {
				var synonym = position.FirmCr.ToLower().Replace("'", "''");
					var result = _producerSynonyms.Select($"Synonym = '{synonym}'");
			//}
			return result;
		}

		private DataRow GetAssortimentOne(FormalizationPosition position)
		{
			var assortmentIds = MonobrendAssortment.Select($"CatalogId = {position.CatalogId}");
			if (assortmentIds.Length != 0) {
				return Assortment.Select($"CatalogId = {position.CatalogId}").FirstOrDefault();
			}
			return null;
		}

		private void CheckExclude(FormalizationPosition position)
		{
			if (position.IsAutomaticProducerSynonym)
				return;

			DataRow[] dr;

			using (_stats.ExludeSearch())
				dr = _excludes.Select(String.Format("CatalogId = {0} and ProducerSynonym = '{1}'",
					position.CatalogId,
					position.FirmCr.Replace("'", "''")));

			//если подходящего исключения нет, то значит позиция должна быть
			//обработана оператором или это не фармацевтика для которой нашелся
			//только синоним без производителя
			if (dr.Length == 0)
				position.Status &= ~UnrecExpStatus.FirmForm;
		}

		public DataRow CreateProducerSynonym(FormalizationPosition position, object producerId, bool count = true)
		{
			var synonym = _producerSynonyms.NewRow();
			if (producerId != null && !(producerId is DBNull)) {
				synonym["CodeFirmCr"] = producerId;
				synonym["IsAutomatic"] = 0;
			} else {
				synonym["CodeFirmCr"] = DBNull.Value;
				if(position.NotCreateUnrecExp) {
					synonym["IsAutomatic"] = 0;
				}
				else {
					synonym["IsAutomatic"] = 1;
				}
			}
			synonym["SynonymFirmCrCode"] = DBNull.Value;
			synonym["Synonym"] = position.FirmCr.Trim();
			synonym["OriginalSynonym"] = position.FirmCr.Trim();
			synonym["Canonical"] = BasePriceParser.SpaceReg.Replace(position.FirmCr, "");
			_producerSynonyms.Rows.Add(synonym);
			if (synonym["CodeFirmCr"] is DBNull && count)
				_stats.ProducerSynonymCreatedCount++;
			return synonym;
		}

		public void Load(MySqlConnection connection)
		{
			var daAssortment = new MySqlDataAdapter("SELECT Id, CatalogId, ProducerId, Checked FROM catalogs.Assortment", connection);
			var excludesBuilder = new MySqlCommandBuilder(daAssortment);
			daAssortment.InsertCommand = excludesBuilder.GetInsertCommand();
			daAssortment.InsertCommand.CommandTimeout = 0;
			Assortment = new DataTable();
			daAssortment.Fill(Assortment);
			_logger.Debug("загрузили Assortment");
			Assortment.PrimaryKey = new[] { Assortment.Columns["CatalogId"], Assortment.Columns["ProducerId"] };
			_logger.Debug("построили индекс по Assortment");

			var daMonobrendAssortment = new MySqlDataAdapter(@"SELECT a.Id, a.CatalogId FROM catalogs.Assortment a join catalogs.catalog c on a.CatalogId = c.Id
where c.Monobrend = 1 and a.Checked = 1", connection);
			MonobrendAssortment = new DataTable();
			daMonobrendAssortment.Fill(MonobrendAssortment);
			_logger.Debug("загрузили монобрендовый Assortment");

			var daForbiddenProducers = new MySqlDataAdapter(@"Select LOWER(a.Name) as Name from farm.ForbiddenProducers a", connection);
			ForbiddenProdusers = new DataTable();
			daForbiddenProducers.Fill(ForbiddenProdusers);
			_logger.Debug("загрузили запрещенные имена производителей");
		}
	}
}