using System;
using System.Data;
using System.IO;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Downloader
{
	public class RostaHandler : AbstractHandler
	{
		private DateTime _lastScan = DateTime.MinValue;
		private TimeSpan _wait = TimeSpan.Zero;

		protected override void ProcessData()
		{
			if (DateTime.Now < _lastScan + _wait)
				return;

			Process();

			_lastScan = DateTime.Now;
			_wait = TimeSpan.FromHours(2) + TimeSpan.FromHours(new Random().NextDouble());
		}

		public static void Process()
		{
			var price = Path.Combine(Settings.Default.TempPath, "price");
			var producers = Path.Combine(Settings.Default.TempPath, "producers");
			try
			{
				if (!Directory.Exists(Settings.Default.TempPath))
					Directory.CreateDirectory(Settings.Default.TempPath);

				if (File.Exists(price))
					File.Delete(price);

				if (File.Exists(producers))
					File.Delete(producers);

				var key = RostaDecoder.GetKey("6B0201010001030201040001000C151E0A091D1C03");
				var loader = new RostaLoader(key);
				loader.DownloadPrice(price, producers);
				using (var connection = new MySqlConnection(Literals.ConnectionString()))
				{
					var id = 1069u;
					var data = PricesValidator.LoadFormRules(id, connection);
					var parser = new FakeRostaParser(price, producers, connection, data);
					connection.Close();
					parser.Formalize();
					if (connection.State == ConnectionState.Closed)
						connection.Open();
					var command = new MySqlCommand("update usersettings.priceitems set PriceDate = now() where id = ?id", connection);
					command.Parameters.AddWithValue("?id", id);
					command.ExecuteNonQuery();
				}
			}
			finally 
			{
				if (File.Exists(price))
					File.Delete(price);
				if (File.Exists(producers))
					File.Delete(producers);
			}
		}
	}
}
