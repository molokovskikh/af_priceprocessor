using System;
using System.Data;
using System.IO;
using Common.Tools;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using System.Configuration;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	public class With
	{
		public static void Connection(Action<MySqlConnection> action)
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();
				action(connection);
			}
		}

		public static T Connection<T>(Func<MySqlConnection, T> action)
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();
				return action(connection);
			}
		}
	}

	public class TestHelper
	{
		public static void InitDirs(params string[] dirs)
		{
			dirs.Each(dir => {
			          	if (Directory.Exists(dir))
			          		Directory.Delete(dir, true);
			          	Directory.CreateDirectory(dir);
			          });
		}

		public static void Execute(string commandText)
		{
			With.Connection(c => {
				var command = c.CreateCommand();
				command.CommandText = commandText;
				command.ExecuteNonQuery();
			});
		}

		public static void Execute(string commandText, params object[] parameters)
		{
			With.Connection(c => {
				var command = c.CreateCommand();
				command.CommandText = String.Format(commandText, parameters);
				command.ExecuteNonQuery();
			});
		}


		public static DataSet Fill(string commandText)
		{
			return With.Connection(c =>
			{
				var adapter = new MySqlDataAdapter(commandText, c);
				var data = new DataSet();
			    adapter.Fill(data);
				return data;
			});
		}


		public static void Formalize<T>(string file) where T : BasePriceParser
		{
			Formalize<T>(file, Convert.ToInt32(Path.GetFileNameWithoutExtension(file)));
		}

		public static void Formalize(Type formatType, string file)
		{
			Formalize(formatType, file, Convert.ToInt32(Path.GetFileNameWithoutExtension(file)));
		}

		public static void Formalize<T>(string file, int priceItemId) where T : BasePriceParser
		{
			Formalize(typeof (T), file, priceItemId);
		}

		public static void Formalize(Type formatType, string file, int priceItemId)
		{
			Formalize(formatType, GetParseRules(priceItemId), file, priceItemId);
		}

		public static void Formalize(Type formatType, DataTable parseRules, string file, int priceItemId)
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				var parser = (BasePriceParser) Activator.CreateInstance(formatType, file, connection, parseRules);
				parser.Formalize();
			}
		}

		public static DataTable GetParseRules(int priceItemId)
		{
			return With.Connection(c => {
				var adapter = new MySqlDataAdapter(@"
select
  pi.Id as PriceItemId,
  pi.RowCount,
  pd.PriceCode,
  PD.PriceName as SelfPriceName,
  PD.PriceType,
  pd.CostType,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  CD.FirmCode,
  CD.ShortName as FirmShortName,
  CD.FirmStatus,
  FR.JunkPos as SelfJunkPos,
  FR.AwaitPos as SelfAwaitPos,
  FR.VitallyImportantMask as SelfVitallyImportantMask,
  ifnull(pd.ParentSynonym, pd.PriceCode) as ParentSynonym,
  PFR.*,
  pricefmts.FileExtention,
  pricefmts.ParserClassName
from
  usersettings.PriceItems pi,
  usersettings.pricescosts pc,
  UserSettings.PricesData pd,
  UserSettings.ClientsData cd,
  Farm.formrules FR,
  Farm.FormRules PFR,
  farm.pricefmts 
where
	pi.Id = ?PriceItemId
and pc.PriceItemId = pi.Id
and pd.PriceCode = pc.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.FirmCode = pd.FirmCode
and FR.Id = pi.FormRuleId
and PFR.Id= if(FR.ParentFormRules, FR.ParentFormRules, FR.Id)
and pricefmts.ID = PFR.PriceFormatId", c);

				adapter.SelectCommand.Parameters.AddWithValue("?PriceItemId", priceItemId);
				var data = new DataSet();
				adapter.Fill(data);
				return data.Tables[0];
			});
		}

		public static void Verify(string priceItemId)
		{
			var row = Fill(String.Format(@"
select pricecode, costcode
from usersettings.pricescosts
where priceitemid = {0}", priceItemId)).Tables[0].Rows[0];

			var pricecode =  row[0];
			var costcode = row[1];

			var etalonCore0 = Fill(String.Format(@"
select c.*, cc.Cost
from core0_copy c
  join corecosts_copy cc on cc.Core_Id = c.Id
where c.pricecode = {0} and cc.pc_costcode = {1};", pricecode, costcode)).Tables[0];
			Verify(priceItemId, etalonCore0);
		}

		public static void Verify(string priceItemId, DataTable etalonCore0)
		{
			var row = Fill(String.Format(@"
select pricecode, costcode
from usersettings.pricescosts
where priceitemid = {0}", priceItemId)).Tables[0].Rows[0];

			var pricecode =  row[0];
			var costcode = row[1];

			var resultCore0 = Fill(String.Format(@"
select c.*, cc.Cost
from core0 c
  join corecosts cc on cc.Core_Id = c.Id
where c.pricecode = {0} and cc.pc_costcode = {1};", pricecode, costcode)).Tables[0];

			Assert.That(resultCore0.Rows.Count, Is.EqualTo(etalonCore0.Rows.Count));
			for(var i = 0; i < etalonCore0.Rows.Count; i++)
			{
				var etalonRow = etalonCore0.Rows[i];
				var resultRow = resultCore0.Rows[i];

				foreach (DataColumn column in etalonCore0.Columns)
				{
					if (column.ColumnName.ToLower() == "id")
						continue;

/*					if (column.ColumnName == "SynonymCode")
					{
						if (Convert.ToInt32(resultRow[column.ColumnName]) != Convert.ToInt32(etalonRow[column.ColumnName]))
							Console.WriteLine(String.Format("Колонка {0}. Строка результата {1}. Строка эталона {2}. Результат {3} эталон {4}", column.ColumnName, resultRow["Id"], etalonRow["Id"], resultRow[column.ColumnName], etalonRow[column.ColumnName]));

						continue;
					}*/


					Assert.That(resultRow[column.ColumnName],
					            Is.EqualTo(etalonRow[column.ColumnName]),
					            "Колонка {0}. Строка результата {1}. Строка эталона {2}.", column.ColumnName, resultRow["Id"], etalonRow["Id"]);
				}
			}
		}
	}
}
