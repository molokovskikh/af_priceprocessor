using Common.MySql;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class BuyingMatrixProcessor
	{
		private ILog _logger = LogManager.GetLogger(typeof(BuyingMatrixProcessor));

		public void UpdateBuyingMatrix(uint priceId)
		{
			With.Connection(c => {
				var command = new MySqlCommand(@"
delete from farm.BuyingMatrix
where priceId = ?priceId;
insert into farm.BuyingMatrix(PriceId, Code, CatalogId, ProducerId)
select c0.PriceCode, c0.Code, p.CatalogId, c0.CodeFirmCr
from farm.Core0 c0
join Catalogs.Products p on p.Id = c0.ProductId
where pricecode = ?priceId
group by p.CatalogId, c0.CodeFirmCr;
", c);
				command.Parameters.AddWithValue("?PriceId", priceId);
				command.ExecuteNonQuery();
			});
			_logger.InfoFormat("Матрица закупок по прайс листу №{0} обновлена", priceId);
		}
	}
}