using System;
using System.Data;
using System.Linq;
using Inforoom.Formalizer;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class ProducerResolver
	{
		private PriceFormalizationInfo _priceInfo;
		private FormalizeStats _stats;

		private DataTable _assortment;
		private DataTable _excludes;
		private DataTable _producerSynonyms;

		public ProducerResolver(PriceFormalizationInfo priceInfo, FormalizeStats stats, DataTable assortment, DataTable excludes, DataTable producerSynonyms)
		{
			_priceInfo = priceInfo;
			_stats = stats;
			_assortment = assortment;
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

			var dr = _producerSynonyms.Select(String.Format("Synonym = '{0}'", position.FirmCr.ToLower().Replace("'", "''")));
			if (dr.Length > 1)
			{
				dr = ResolveAmbiguous(position, dr);
			}

			if (dr.Length == 0)
			{
				position.IsAutomaticProducerSynonym = true;
				position.InternalProducerSynonymId = InsertSynonymFirm(position);
				return;
			}

			//Если значение CodeFirmCr не установлено, то устанавливаем в null, иначе берем значение кода
			position.CodeFirmCr = Convert.IsDBNull(dr[0]["CodeFirmCr"]) ? null : (long?)Convert.ToInt64(dr[0]["CodeFirmCr"]);
			position.IsAutomaticProducerSynonym = Convert.ToBoolean(dr[0]["IsAutomatic"]);
			if (Convert.IsDBNull(dr[0]["InternalProducerSynonymId"]))
			{
				_stats.ProducerSynonymUsedExistCount++;
				position.SynonymFirmCrCode = Convert.ToInt64(dr[0]["SynonymFirmCrCode"]);
			}
			else
				position.InternalProducerSynonymId = Convert.ToInt64(dr[0]["InternalProducerSynonymId"]);

			if (!position.IsAutomaticProducerSynonym)
				position.AddStatus(UnrecExpStatus.FirmForm);

			CheckAssortmentStatus(position);
		}

		private DataRow[] ResolveAmbiguous(FormalizationPosition position, DataRow[] productSynonyms)
		{
			var assortment = this._assortment.Select(String.Format("CatalogId = {0}", position.CatalogId));
			foreach (var productSynonym in productSynonyms)
			{
				if (productSynonym["CodeFirmCr"] is DBNull)
					continue;

				if (assortment.Any(a => Convert.ToUInt32(a["ProducerId"]) == Convert.ToUInt32(productSynonym["CodeFirmCr"])))
					return new [] {productSynonym};
			}
			return productSynonyms;
		}

		private void CheckAssortmentStatus(FormalizationPosition position)
		{
			if (!position.ProductId.HasValue || !position.CodeFirmCr.HasValue)
				return;

			var assortmentStatus = GetAssortmentStatus(position);
			//Если получили исключение, то сбрасываем CodeFirmCr
			if (assortmentStatus == UnrecExpStatus.MarkExclude)
				position.CodeFirmCr = null;
			position.AddStatus(assortmentStatus);
		}

		private UnrecExpStatus GetAssortmentStatus(FormalizationPosition position)
		{
			DataRow[] dr;

			using(_stats.AssortmentSearch())
				dr = _assortment.Select(String.Format("CatalogId = {0} and ProducerId = {1}",
					position.CatalogId,
					position.CodeFirmCr));

			if (dr.Length == 1)
				return UnrecExpStatus.AssortmentForm;

			using(_stats.ExludeSearch())
				dr = _excludes.Select(String.Format("CatalogId = {0} and ProducerSynonym = '{1}'",
					position.CatalogId,
					position.FirmCr.Replace("'", "''")));

			//Если мы ничего не нашли, то добавляем в исключение
			if (dr.Length == 0 && _priceInfo.PricePurpose == PricePurpose.Normal)
				CreateExclude(position);

			return UnrecExpStatus.MarkExclude;
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

		private long InsertSynonymFirm(FormalizationPosition position)
		{
			var drInsert = _producerSynonyms.NewRow();
			drInsert["CodeFirmCr"] = DBNull.Value;
			drInsert["SynonymFirmCrCode"] = DBNull.Value;
			drInsert["IsAutomatic"] = 1;
			drInsert["Synonym"] = position.FirmCr.ToLower();
			drInsert["OriginalSynonym"] = position.FirmCr.Trim();
			_producerSynonyms.Rows.Add(drInsert);
			if (position.Core != null)
				position.Core.CreatedProducerSynonym = drInsert;
			_stats.ProducerSynonymCreatedCount++;
			return (long)drInsert["InternalProducerSynonymId"];
		}
	}
}