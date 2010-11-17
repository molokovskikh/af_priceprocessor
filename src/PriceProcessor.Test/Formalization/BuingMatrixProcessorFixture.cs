using System;
using Common.MySql;
using Inforoom.PriceProcessor.Formalizer;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class BuingMatrixProcessorFixture
	{
		[Test]
		public void Buying_matrix_should_update()
		{
			var priceId = 4957u;
			With.Connection(c => {
				var command = new MySqlCommand("delete from farm.BuyingMatrix where PriceId = ?Priceid", c);
				command.Parameters.AddWithValue("?PriceId", priceId);
				command.ExecuteNonQuery();
			});

			new BuyingMatrixProcessor().UpdateBuyingMatrix(priceId);
			With.Connection(c => {
				var command = new MySqlCommand("select count(*) from farm.BuyingMatrix where PriceId = ?Priceid", c);
				command.Parameters.AddWithValue("?PriceId", priceId);
				var count = Convert.ToUInt32(command.ExecuteScalar());
				Assert.That(count, Is.GreaterThan(0));
			});
		}
	}
}