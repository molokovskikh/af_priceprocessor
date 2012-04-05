using System.Collections.Generic;
using System.Data;
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
		public UniversalFormalizer(string filename, MySqlConnection connection, DataTable data)
			: base(filename, connection, data)
		{}

		public void Formalize()
		{
			using(var stream = File.OpenRead(_fileName)) {
				var reader = new UniversalReader(stream);

				var settings = reader.Settings().ToList();

				FormalizePrice(reader);
				With.Connection(c => {
					var command = new MySqlCommand("", c);
					UpdateIntersection(command, settings, reader.CostDescriptions);
				});
			}
		}

		public IList<string> GetAllNames()
		{
			throw new System.NotImplementedException();
		}
	}
}