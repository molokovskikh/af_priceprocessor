using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MySql.Data.MySqlClient;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class WaybillClientExistsFixture
	{
		private static int[] _existsInAddresses = new int[4] { 1, 2, 3, 4};

		private static int[] _notExistsInAddresses = new int[4] { 5, 1030, 1044, 1304 };

		private static int[] _existsInClientsData = new int[4] {42, 44, 45, 46};

		private static int[] _notExistsInClientsData = new int[4] { 2322, 2321, 5401, 5408 };

		[TestFixtureSetUp]
		public void TestPrepareIds()
		{
			var queryUpdateClientsData = @"
UPDATE usersettings.ClientsData cd
SET cd.FirmType = 1
WHERE cd.FirmCode = ?FirmCode
";
			foreach (var id in _existsInClientsData)
			{
				var paramFirmCode = new MySqlParameter("?FirmCode", id);
				With.Connection(connection => {
					MySqlHelper.ExecuteNonQuery(connection, queryUpdateClientsData, paramFirmCode);
				});
				Assert.IsTrue(true, "Удалось обновить существующих в ClientsData");
			}

			queryUpdateClientsData = @"
UPDATE usersettings.ClientsData cd
SET cd.FirmType = 0
WHERE cd.FirmCode = ?FirmCode
";
			foreach (var id in _notExistsInClientsData)
			{
				var paramFirmCode = new MySqlParameter("?FirmCode", id);
				With.Connection(connection =>
				{
					MySqlHelper.ExecuteNonQuery(connection, queryUpdateClientsData, paramFirmCode);
				});
				Assert.IsTrue(true, "Удалось обновить НЕ существующих в ClientsData");
			}
			foreach (var id in _notExistsInAddresses)
			{
				var paramFirmCode = new MySqlParameter("?FirmCode", id);
				With.Connection(connection =>
				{
					MySqlHelper.ExecuteNonQuery(connection, queryUpdateClientsData, paramFirmCode);
				});
				Assert.IsTrue(true, "Удалось обновить НЕ существующих в ClientsData");
			}
			
			// Удаляем записи из future.Addresses, чтобы их там точно не было
			var queryDeleteFromAddresses = @"
DELETE FROM future.addresses
WHERE (Id = ?AddrId) OR (LegacyId = ?AddrId)
";
			foreach (var id in _notExistsInAddresses)
			{
				var paramAddrId = new MySqlParameter("?AddrId", id);
				try
				{
					With.Connection(connection => {
						MySqlHelper.ExecuteNonQuery(connection, queryDeleteFromAddresses, paramAddrId);
					});
				}
				catch (Exception)
				{}
			}			
		}

		[Test]
		public void TestClientExists()
		{
			IEnumerable<int> existsIds = new List<int>();
			existsIds = _existsInAddresses.Concat(_existsInClientsData);
			IEnumerable<int> notExistsIds = new List<int>();
			notExistsIds = _notExistsInAddresses.Concat(_notExistsInClientsData);
			var queryGetClientCode = @"
SELECT COUNT(*)
FROM (
	SELECT cd.FirmCode 
	FROM usersettings.ClientsData cd
	WHERE cd.FirmType = 1 AND FirmCode = ?clientCode
	UNION
	SELECT Addr.Id
	FROM Future.Addresses Addr
	WHERE Addr.Id = ?clientCode OR Addr.LegacyId = ?clientCode
) clients";
			foreach (var id in existsIds)
			{
				var paramClientCode = new MySqlParameter("?clientCode", id);
				int countClients = With.Connection<int>(connection => {
					return Convert.ToInt32(MySqlHelper.ExecuteScalar(
						connection, queryGetClientCode, paramClientCode));
				});
				Assert.IsTrue(countClients == 1, String.Format(
					"Клиент существует, однако не был выбран. Код клиента: {0}", id));
			}

			foreach (var id in notExistsIds)
			{
				var paramClientCode = new MySqlParameter("?clientCode", id);
				int countClients = With.Connection<int>(connection =>
				{
					return Convert.ToInt32(MySqlHelper.ExecuteScalar(
						connection, queryGetClientCode, paramClientCode));
				});
				Assert.IsTrue(countClients == 0, String.Format(
					"Клиент не существует, однако был выбран. Код клиента: {0}", id));
			}			
		}
	}
}
