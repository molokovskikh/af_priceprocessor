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
		private ILog _logger = LogManager.GetLogger(typeof (ProducerResolver));

		private PriceFormalizationInfo _priceInfo;
		private FormalizeStats _stats;

		private DataTable _assortment;
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

			if (String.IsNullOrEmpty(position.FirmCr))
			{
				position.AddStatus(UnrecExpStatus.FirmForm);
				return;
			}

			var synonym = ResolveSynonym(position);
			if (synonym == null)
				synonym = CreateProducerSynonym(position);

			if (!Convert.IsDBNull(synonym["CodeFirmCr"]))
				position.CodeFirmCr = Convert.ToInt64(synonym["CodeFirmCr"]);
			position.IsAutomaticProducerSynonym = Convert.ToBoolean(synonym["IsAutomatic"]);
			if (Convert.IsDBNull(synonym["InternalProducerSynonymId"]))
			{
				_stats.ProducerSynonymUsedExistCount++;
				position.SynonymFirmCrCode = Convert.ToInt64(synonym["SynonymFirmCrCode"]);
			}
			else
			{
				position.InternalProducerSynonymId = Convert.ToInt64(synonym["InternalProducerSynonymId"]);
				if (position.Core != null)
					position.Core.CreatedProducerSynonym = synonym;
			}

			if (!position.IsAutomaticProducerSynonym)
				position.AddStatus(UnrecExpStatus.FirmForm);

			if (position.CodeFirmCr == null)
				CheckExclude(position);
		}

		private DataRow ResolveSynonym(FormalizationPosition position)
		{
			var synonyms = _producerSynonyms.Select(String.Format("Synonym = '{0}'", position.FirmCr.ToLower().Replace("'", "''")));
			var assortment = _assortment.Select(String.Format("CatalogId = {0}", position.CatalogId));
			foreach (var productSynonym in synonyms)
			{
				if (productSynonym["CodeFirmCr"] is DBNull)
					continue;

				using (_stats.AssortmentSearch())
				{
					if (assortment.Any(a => Convert.ToUInt32(a["ProducerId"]) == Convert.ToUInt32(productSynonym["CodeFirmCr"])))
					{
						position.AddStatus(UnrecExpStatus.AssortmentForm);
						return productSynonym;
					}
				}
			}
			return synonyms.FirstOrDefault(s => s["CodeFirmCr"] is DBNull);
		}

		private void CheckExclude(FormalizationPosition position)
		{
			if (position.IsAutomaticProducerSynonym)
				return;

			DataRow[] dr;

			using(_stats.ExludeSearch())
				dr = _excludes.Select(String.Format("CatalogId = {0} and ProducerSynonym = '{1}'",
					position.CatalogId,
					position.FirmCr.Replace("'", "''")));
/*
			//Если мы ничего не нашли, то добавляем в исключение
			if (dr.Length == 0 && _priceInfo.PricePurpose == PricePurpose.Normal)
				CreateExclude(position);
 */
			if (dr.Length > 1)
				position.AddStatus(UnrecExpStatus.AssortmentForm);
		}

		private void CreateExclude(FormalizationPosition position)
		{
			try
			{
				var drExclude = _excludes.NewRow();
				drExclude["PriceCode"] = _priceInfo.ParentSynonym;
				drExclude["CatalogId"] = position.CatalogId.Value;
				drExclude["ProducerSynonym"] = position.FirmCr;
				drExclude["OriginalSynonymId"] = position.SynonymCode;
				_excludes.Rows.Add(drExclude);
			}
			catch (ConstraintException)
			{}
		}

		private DataRow CreateProducerSynonym(FormalizationPosition position)
		{
			var synonym = _producerSynonyms.NewRow();
			synonym["CodeFirmCr"] = DBNull.Value;
			synonym["SynonymFirmCrCode"] = DBNull.Value;
			synonym["IsAutomatic"] = 1;
			synonym["Synonym"] = position.FirmCr;
			synonym["OriginalSynonym"] = position.FirmCr;
			_producerSynonyms.Rows.Add(synonym);
			_stats.ProducerSynonymCreatedCount++;
			return synonym;
		}

		public void Load(MySqlConnection connection)
		{
			var daAssortment = new MySqlDataAdapter("SELECT Id, CatalogId, ProducerId, Checked FROM catalogs.Assortment where Checked = 1", connection);
			var excludesBuilder  = new MySqlCommandBuilder(daAssortment);
			daAssortment.InsertCommand = excludesBuilder.GetInsertCommand();
			daAssortment.InsertCommand.CommandTimeout = 0;
			_assortment = new DataTable();
			daAssortment.Fill(_assortment);
			_logger.Debug("загрузили Assortment");
			_assortment.PrimaryKey = new[] { _assortment.Columns["CatalogId"], _assortment.Columns["ProducerId"] };
			_logger.Debug("построили индекс по Assortment");
		}
	}
}