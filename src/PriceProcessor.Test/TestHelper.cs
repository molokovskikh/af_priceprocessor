using System;
using System.Data;
using System.IO;
using Common.Tools;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using System.Configuration;

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

		public static void Formilize<T>(string file, int priceItemId) where T : BasePriceParser
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
			{
				connection.Open();
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
and pricefmts.ID = PFR.PriceFormatId", connection);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceItemId", priceItemId);
				var data = new DataSet();
				adapter.Fill(data);
				connection.Close();
				var parser = (T) Activator.CreateInstance(typeof (T), file, connection, data.Tables[0]);
				parser.Formalize();
			}
		}
	}
}
