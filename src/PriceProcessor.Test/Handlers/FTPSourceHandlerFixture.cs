using System;
using LumiSoft.Net.FTP.Server;
using NUnit.Framework;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class FTPSourceHandlerFixture
	{
		[Test]
		public void SubtractTotalMinutes()
		{
			var lastDateTime = DateTime.Now;
			var prevDateTime = DateTime.Now.AddHours(-3);
			Assert.That(lastDateTime.Subtract(prevDateTime).TotalMinutes > 0, "Получилось не положительное число");
			Assert.That(prevDateTime.Subtract(lastDateTime).TotalMinutes < 0, "Получилось не отрицательное число");
		}

		[Test]
		public void Download_file_from_ftp_server()
		{
			var ftpServer = new FTP_Server();
			ftpServer.StartServer();
			ftpServer.StopServer();
		}
	}
}