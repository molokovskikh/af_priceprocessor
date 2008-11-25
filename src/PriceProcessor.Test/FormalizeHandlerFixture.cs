using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using System.IO;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data;


namespace PriceProcessor.Test
{
	[TestFixture]
	public class FormalizeHandlerFixture
	{
		[Test(Description = "Метод для создания большого кол-ва временных директорий во временной папке. Предполагалось, что большое кол-во папок тормозит работу OleDbProvider'а.")]
		public void CreateTempDirectories()
		{
			//string _rootTempPath = Path.GetTempPath();

			//string[] _dirs = Directory.GetDirectories(_rootTempPath);

			//Assert.That(_dirs.Length < 2000, "Во временной директории уже и так много папок. Удалите лишнее.");

			//List<string> _priceItems = new List<string>();
			//DataSet dsPriceItems = MySqlHelper.ExecuteDataset(
			//    ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
			//    "select Id from usersettings.PriceItems");
			//foreach (DataRow drPriceItem in dsPriceItems.Tables[0].Rows)
			//{
			//    _priceItems.Add(drPriceItem["Id"].ToString());
			//    Directory.CreateDirectory(_rootTempPath + drPriceItem["Id"].ToString());
			//}

			//int _maxDownId = Convert.ToInt32(MySqlHelper.ExecuteScalar(
			//    ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
			//    "SELECT max(RowId) FROM `logs`.downlogs d"));

			//int _maxDirectoryCount = 200000;

			//Random _random = new Random();

			//while (_maxDirectoryCount > 0)
			//{
			//    int _priceItemIndex = _random.Next(0, _priceItems.Count);
			//    Directory.CreateDirectory(_rootTempPath + "d" + _priceItems[_priceItemIndex] + "_" + (_maxDownId - _maxDirectoryCount).ToString());
			//    if (_maxDirectoryCount % 10000 == 0)
			//        Console.WriteLine("_maxDirectoryCount = {0}", _maxDirectoryCount);
			//    _maxDirectoryCount--;
			//}
		}
	}
}
