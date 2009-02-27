using System;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor
{
	class MySqlUtils
	{
		public static void InTransaction(Action<MySqlConnection, MySqlTransaction> action)
		{
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
		}
	}
}
