using System;
using MySql.Data.MySqlClient;
using Common.MySql;

namespace Inforoom.PriceProcessor
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
	}
}