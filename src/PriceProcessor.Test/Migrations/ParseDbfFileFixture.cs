using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Formalizer;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Configuration;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("не работает, т.к. нужны были для проверки формализации новых форматов и сравнения со старыми")]
	public class ParseDbfFileFixture
	{
		[Test]
		public void Parse_order_with_native_dbf_parser_should_be_same_as_oledb()
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();
				TestHelper.Formalize<NativeDbfPriceParser>(Path.GetFullPath(@".\Data\552.dbf"), 552);

				var result = new DataSet();
				var adapter = new MySqlDataAdapter(@"
select *
from farm.core0 c
  join corecosts cc on c.id = cc.Core_Id
where pricecode = 3331", connection);
				adapter.Fill(result);

				var etalon = new DataSet();
				etalon.ReadXml(@".\Data\552.xml", XmlReadMode.ReadSchema);
				Compare(result, etalon);
			}
		}

		private void Compare(DataSet result, DataSet etalon)
		{
			Assert.That(result.Tables.Count, Is.EqualTo(1), "нет таблицы с данными");
			Assert.That(etalon.Tables.Count, Is.EqualTo(1), "нет таблицы с данными");
			Assert.That(result.Tables[0].Rows.Count, Is.EqualTo(etalon.Tables[0].Rows.Count));
			Assert.That(result.Tables[0].Rows.Count, Is.GreaterThan(0));
			for (int i = 0; i < result.Tables[0].Rows.Count; i++)
			{
				var resultRow = result.Tables[0].Rows[i];
				var etalonRow = etalon.Tables[0].Rows[i];
				foreach (DataColumn column in result.Tables[0].Columns)
				{
					//junk - определяется на основании текущей даты по этому со временем значение может измениться
					if (column.ColumnName == "Id" || column.ColumnName == "Core_Id" || column.ColumnName == "Junk" ||
						column.ColumnName == "UpdateTime" || column.ColumnName == "QuantityUpdate" || column.ColumnName == "ProducerCost")
						continue;
					Assert.That(resultRow[column.ColumnName], Is.EqualTo(etalonRow[column.ColumnName]), "не сошлось значение в колонке {0}, строка {1}", column.ColumnName, i);
				}
			}
		}

		[Test, Ignore]
		public void BuildEtalon()
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();
				var adapter = new MySqlDataAdapter(@"
select *
from farm.core0 c
  join corecosts cc on c.id = cc.Core_Id
where pricecode = 3331", connection);
				var data = new DataSet();
				adapter.Fill(data);
				using (var file = File.Create("552.xml"))
				{
					data.WriteXml(file, XmlWriteMode.WriteSchema);
				}
			}
		}
	}
}
