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
				.InsertInto("Farm.Buyingmatrix", "MatrixId, PriceId, Code, ProductId, ProducerId, CodeOKP")
				.Select("?matrixId, c0.PriceCode, c0.Code, c0.ProductId, c0.CodeFirmCr, c0.CodeOKP")
				.From("farm.Core0 c0")
				.Where("c0.pricecode = ?priceId")
				.GroupBy("c0.ProductId, c0.CodeFirmCr, c0.CodeOKP");
			if (price.CodeOkpFilterPrice != null) {
				query.Join("join Farm.Core0 co on co.CodeOKP = c0.CodeOKP");
				query.Where("c0.CodeFirmCr is not null",
					"co.PriceCode = ?okpPriceId");
			}

			With.Transaction((c, t) => {
				var sql = @"
delete from farm.BuyingMatrix
where priceId = ?priceId;

update Usersettings.AnalitFReplicationInfo r
	join Customers.Users u on u.Id = r.UserId
		join Usersettings.RetClientsSet rcs on rcs.ClientCode = u.ClientId
set r.ForceReplication = 1
where rcs.BuyingMatrix = ?MatrixId
	or rcs.OfferMatrix = ?MatrixId;
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