using System;
using Common.MySql;
using Inforoom.PriceProcessor.Models;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class BuyingMatrixProcessor
	{
		private ILog _logger = LogManager.GetLogger(typeof(BuyingMatrixProcessor));

		public void UpdateBuyingMatrix(Price price)
		{
			if (price.Matrix == null)
				return;

			var query = new Query();
			query
				.InsertInto("Farm.Buyingmatrix", "MatrixId, PriceId, Code, ProductId, ProducerId")
				.Select("?matrixId, c0.PriceCode, c0.Code, c0.ProductId, c0.CodeFirmCr")
				.From("farm.Core0 c0")
				.Where("c0.pricecode = ?priceId")
				.GroupBy("c0.ProductId, c0.CodeFirmCr");
			if (price.CodeOkpFilterPrice != null) {
				query.InsertIntoParts.Add("CodeOKP");
				query.Select("c0.CodeOKP");
				query.Join("join Farm.Core0 co on co.CodeOKP = c0.CodeOKP");
				query.Where("co.CodeOKP is not null",
					"c0.CodeOKP is not null",
					"c0.CodeFirmCr is not null",
					"co.PriceCode = ?okpPriceId");
				query.GroupBy("c0.CodeOKP");
			}

			With.Connection(c => {
				var sql = @"
delete from farm.BuyingMatrix
where priceId = ?priceId;
";
				sql += query.ToSql() + ";";
				var command = new MySqlCommand(sql, c);
				command.Parameters.AddWithValue("?PriceId", price.Id);
				command.Parameters.AddWithValue("?MatrixId", price.Matrix.Id);
				if (price.CodeOkpFilterPrice != null)
					command.Parameters.AddWithValue("?okpPriceId", price.CodeOkpFilterPrice.Id);
				command.ExecuteNonQuery();
			});
			_logger.InfoFormat("Матрица закупок по прайс листу №{0} обновлена", price.Id);
		}
	}
}