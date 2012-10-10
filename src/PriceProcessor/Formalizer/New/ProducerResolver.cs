using System;
using System.Data;
using System.Linq;
using Inforoom.Formalizer;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class ProducerResolver
	{
		private ILog _logger = LogManager.GetLogger(typeof(ProducerResolver));

		private PriceFormalizationInfo _priceInfo;
		private FormalizeStats _stats;

		public DataTable Assortment;
		private DataTable _excludes;
		private DataTable _producerSynonyms;

		public ProducerResolver(PriceFormalizationInfo priceInfo, FormalizeStats stats, DataTable excludes, DataTable producerSynonyms)
		{
			_priceInfo = priceInfo;
			_stats = stats;
			_excludes = excludes;
			_producerSynonyms = producerSynonyms;
		}

		public void ResolveProducer(FormalizationPosition position)
		{
			if (!position.IsSet(UnrecExpStatus.NameForm))
				return;

			if (String.IsNullOrEmpty(position.FirmCr)) {
				position.AddStatus(UnrecExpStatus.FirmForm);
				return;
			}

			var synonym = Resolve(position);
			if (synonym == null)
				synonym = CreateProducerSynonym(position);

			if (!Convert.IsDBNull(synonym["CodeFirmCr"]))
				position.CodeFirmCr = Convert.ToInt64(synonym["CodeFirmCr"]);
			position.IsAutomaticProducerSynonym = Convert.ToBoolean(synonym["IsAutomatic"]);
			if (Convert.IsDBNull(synonym["InternalProducerSynonymId"])) {
				_stats.ProducerSynonymUsedExistCount++;
				position.SynonymFirmCrCode = Convert.ToInt64(synonym["SynonymFirmCrCode"]);
			}
			else {
				position.InternalProducerSynonymId = Convert.ToInt64(synonym["InternalProducerSynonymId"]);
				if (position.Core != null)
					position.Core.CreatedProducerSynonym = synonym;
			}

			if (!position.IsAutomaticProducerSynonym)
				position.AddStatus(UnrecExpStatus.FirmForm);

/*
			if (position.CodeFirmCr != null && !position.Pharmacie.Value)
				CheckAndCreateAssortment(position);
*/

			if (position.CodeFirmCr == null)
				CheckExclude(position);
		}

		private void CheckAndCreateAssortment(FormalizationPosition position)
		{
			if (Assortment.Rows
				.Cast<DataRow>()
				.Any(r => Convert.ToUInt32(r["CatalogId"]) == Convert.ToUInt32(position.CatalogId.Value)
					&& Convert.ToUInt32(r["ProducerId"]) == Convert.ToUInt32(position.CodeFirmCr.Value)))
				return;

			var assortment = Assortment.NewRow();
			assortment["CatalogId"] = position.CatalogId;
			assortment["ProducerId"] = position.CodeFirmCr;
			assortment["Checked"] = false;
			Assortment.Rows.Add(assortment);
		}


		public void Update(MySqlConnection connection)
		{
			var da = new MySqlDataAdapter("SELECT Id, CatalogId, ProducerId, Checked FROM catalogs.Assortment ", connection);
			var excludesBuilder = new MySqlCommandBuilder(da);
			da.InsertCommand = excludesBuilder.GetInsertCommand();
			da.InsertCommand.CommandTimeout = 0;
			da.Update(Assortment);
		}

		public DataRow Resolve(FormalizationPosition position)
		{
			if (position.Pharmacie.Value)
				return ResolveWithAssortmentRespect(position);
			else
				return ResolveIgnoreAssortment(position);
		}

		private DataRow ResolveIgnoreAssortment(FormalizationPosition position)
		{
			var synonyms = _producerSynonyms.Select(String.Format("Synonym = '{0}'", position.FirmCr.ToLower().Replace("'", "''")));
			//предпочитаем синонимы с производителем
			var synonym = synonyms.FirstOrDefault(s => !(s["CodeFirmCr"] is DBNull));
			if (synonym != null)
				return synonym;

			return synonyms.FirstOrDefault();
		}

		private DataRow ResolveWithAssortmentRespect(FormalizationPosition position)
		{
			var synonyms = _producerSynonyms.Select(String.Format("Synonym = '{0}'", position.FirmCr.ToLower().Replace("'", "''")));
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
		}

		private DataRow GetAssortimentOne(FormalizationPosition position)
		{
			var assortiments = Assortment.Select(String.Format("CatalogId = {0} and Checked = 1", position.CatalogId));
			if (assortiments.Length == 1)
				return assortiments[0];
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
/*
			//Если мы ничего не нашли, то добавляем в исключение
			if (dr.Length == 0 && _priceInfo.PricePurpose == PricePurpose.Normal)
				CreateExclude(position);
 */
			//если подходящего исключения нет, то значит позиция должна быть
			//обработана оператором или это не фармацевтика для которой нашелся 
			//только синоним без производителя
			if (dr.Length == 0)
				position.Status &= ~UnrecExpStatus.FirmForm;
		}

		private void CreateExclude(FormalizationPosition position)
		{
			try {
				var drExclude = _excludes.NewRow();
				drExclude["PriceCode"] = _priceInfo.ParentSynonym;
				drExclude["CatalogId"] = position.CatalogId.Value;
				drExclude["ProducerSynonym"] = position.FirmCr;
				drExclude["OriginalSynonymId"] = position.SynonymCode;
				_excludes.Rows.Add(drExclude);
			}
			catch (ConstraintException) {
			}
		}

		private DataRow CreateProducerSynonym(FormalizationPosition position)
		{
			var synonym = _producerSynonyms.NewRow();
			var assortiment = GetAssortimentOne(position);
			if (assortiment != null) {
				synonym["CodeFirmCr"] = assortiment["ProducerId"];
				synonym["IsAutomatic"] = 0;
			}
			else {
				synonym["CodeFirmCr"] = DBNull.Value;
				synonym["IsAutomatic"] = 1;
			}
			synonym["SynonymFirmCrCode"] = DBNull.Value;
			synonym["Synonym"] = position.FirmCr;
			synonym["OriginalSynonym"] = position.FirmCr;
			_producerSynonyms.Rows.Add(synonym);
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
		}
	}
}