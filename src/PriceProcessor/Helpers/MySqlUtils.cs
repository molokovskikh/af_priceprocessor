using System;
using System.Data;
using Common.MySql;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Helpers
{
	public class MySqlUtils
	{
		public static void InTransaction(Action<MySqlConnection> action)
		{
			With.DeadlockWraper(() => With.Transaction((c, t) => action(c)));
		}

		public static void InTransaction(Action<MySqlConnection, MySqlTransaction> action)
		{
			With.DeadlockWraper(() => With.Transaction(action));
		}

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