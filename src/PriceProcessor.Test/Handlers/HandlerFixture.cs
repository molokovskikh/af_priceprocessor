using System.Data;
using Common.MySql;
using Inforoom.Downloader;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class HandlerFixture
	{
		[Test]
		public void Get_sources()
		{
			With.Connection(c => {
				var adapter = new MySqlDataAdapter(
					BaseSourceHandler.GetSourcesCommand("HTTP"),
					c);
				var table = new DataTable();
				adapter.Fill(table);
			});
			
		}
	}
}