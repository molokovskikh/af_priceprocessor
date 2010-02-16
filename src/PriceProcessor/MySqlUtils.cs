using System;
using MySql.Data.MySqlClient;
using Common.MySql;

namespace Inforoom.PriceProcessor
{
	class MySqlUtils
	{
		public static void InTransaction(Action<MySqlConnection, MySqlTransaction> action)
		{
			With.DeadlockWraper(() => {
        		using (var connection = new MySqlConnection(Literals.ConnectionString()))
        		{
        			connection.Open();
        			var transaction = connection.BeginTransaction();
        			try
        			{
        				action(connection, transaction);
        				transaction.Commit();
        			}
        			catch
        			{
        				transaction.Rollback();
        				throw;
        			}
        		}
        	}, 5);
		}

		public static void InTransaction(Action<MySqlConnection> action)
		{
			InTransaction((c, t) => action(c));
		}

	}
}
