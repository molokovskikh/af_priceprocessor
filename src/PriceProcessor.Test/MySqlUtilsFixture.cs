using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Helpers;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class MySqlUtilsFixture
	{
		[Test]
		public void If_deadlock_occur_try_repeat_5_times()
		{
			int countRepeat = 5;
			var i = 0;

			try {
				MySqlUtils.InTransaction(helper => {
					i++;
					throw GetMySqlException(1205, "");
				});
				Assert.Fail("Должны были выбросить исключение");
			}
			catch (MySqlException e) {
				Assert.That(e.Number, Is.EqualTo(1205), e.ToString());
			}
			Assert.That(i, Is.EqualTo(countRepeat));
		}

		public static MySqlException GetMySqlException(int errorCode, string message)
		{
			return (MySqlException)typeof(MySqlException)
				.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
				null,
				new[] { typeof(string), typeof(int) },
				null)
				.Invoke(new object[] { message, errorCode });
		}
	}
}