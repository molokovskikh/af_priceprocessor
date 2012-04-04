using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class UniversalFormalizer : BaseFormalizer, IPriceFormalizer
	{
		public void Formalize()
		{
			using(var stream = File.OpenRead(_fileName)) {
				var reader = new UniversalReader(stream);

				var settings = reader.Settings().ToList();
				var costId = ((uint)_priceInfo.CostCode);
				PriceCost cost;
				using(new SessionScope())
					cost = PriceCost.Find(costId);

				FormalizePrice(reader, cost);
				With.Connection(c => {
					var command = new MySqlCommand("", c);
					UpdateIntersection(command, cost, settings);
				});
			}
		}

		public IList<string> GetAllNames()
		{
			throw new System.NotImplementedException();
		}
	}
}