using System;
using System.Data;
using Common.MySql;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Helpers
{
	public class MySqlUtils
	{
		public static DataTable Fill(string sql)
		{
			return With.Connection(c => {
				var adapter = new MySqlDataAdapter(sql, c);
				var table = new DataTable();
				adapter.Fill(table);
				return table;
			});
		}
	}
}