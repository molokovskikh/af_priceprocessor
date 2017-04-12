using System;
using System.Data;
using System.Linq;
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
		private BasePriceParser.Barcode[] barcodes;

		public ProducerResolver(FormalizeStats stats, DataTable excludes, DataTable producerSynonyms, BasePriceParser.Barcode[] barcodes)
		{
			_stats = stats;
			_excludes = excludes;
			_producerSynonyms = producerSynonyms;
			this.barcodes = barcodes;
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
			if (synonym == null) {
				var producerId = GetAssortimentOne(position)?["ProducerId"];
				if (producerId == null && !String.IsNullOrWhiteSpace(position.Core.EAN13))
					producerId = barcodes.FirstOrDefault(x => x.Value == position.Core.EAN13)?.ProducerId;
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

		public DataRow CreateProducerSynonym(FormalizationPosition position, object producerId)
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
			synonym["Synonym"] = position.FirmCr;
			synonym["OriginalSynonym"] = position.FirmCr;
			_producerSynonyms.Rows.Add(synonym);
			if (synonym["CodeFirmCr"] is DBNull)
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