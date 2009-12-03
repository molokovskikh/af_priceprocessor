using System;
using System.Data;
using System.IO;
using System.Text;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Client;
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

		/// <summary>
		/// Кладет сообщение с файлом-вложением в IMAP папку.
		/// Ящик, пароль и название IMAP папки берутся из конфигурационного файла.
		/// </summary>
		/// <param name="to">Адрес, который будет помещен в поле TO</param>
		/// <param name="from">Адрес, который будет помещен в поле FROM</param>
		/// <param name="attachFilePath">Путь к файлу, который будет помещен во вложение к этому письму</param>
		public static void StoreMessageWithAttachToImapFolder(string mailbox, string password, string folder, 
			string to, string from, string attachFilePath)
		{
			var templateMessageText = @"To: {0}
From: {1}
Subject: TestWaybillSourceHandler
Content-Type: multipart/mixed;
 boundary=""------------060602000201050608050809""

This is a multi-part message in MIME format.
--------------060602000201050608050809
Content-Type: text/plain; charset=UTF-8; format=flowed
Content-Transfer-Encoding: 7bit



--------------060602000201050608050809
Content-Type: application/octet-stream;
 name=""{2}""
Content-Transfer-Encoding: base64
Content-Disposition: attachment;
 filename=""{2}""

{3}
--------------060602000201050608050809--

";
			using (var fileStream = File.OpenRead(attachFilePath))
			{
				var fileBytes = new byte[fileStream.Length];
				fileStream.Read(fileBytes, 0, (int)(fileStream.Length));
				var messageText = String.Format(templateMessageText, to, from,
												Path.GetFileName(attachFilePath), Convert.ToBase64String(fileBytes));
				byte[] messageBytes = new UTF8Encoding().GetBytes(messageText);
				using (var imapClient = new IMAP_Client())
				{
					imapClient.Connect(Settings.Default.IMAPHost, Convert.ToInt32(Settings.Default.IMAPPort));
					imapClient.Authenticate(mailbox, password);
					imapClient.StoreMessage(folder, messageBytes);
				}
			}
		}

		/// <summary>
		/// Удаляет все сообщения из IMAP папки
		/// </summary>
		public static void ClearImapFolder(string mailbox, string password, string folder)
		{
			using (var imapClient = new IMAP_Client())
			{
				imapClient.Connect(Settings.Default.IMAPHost, Convert.ToInt32(Settings.Default.IMAPPort));
				imapClient.Authenticate(mailbox, password);
				imapClient.SelectFolder(folder);
				var sequenceSet = new IMAP_SequenceSet();
				sequenceSet.Parse("1:*", long.MaxValue);
				var items = imapClient.FetchMessages(sequenceSet, IMAP_FetchItem_Flags.UID, false, false);
				if ((items != null) && (items.Length > 0))
				{
					foreach (var item in items)
					{
						var sequenceMessages = new IMAP_SequenceSet();
						sequenceMessages.Parse(item.UID.ToString(), long.MaxValue);
						imapClient.DeleteMessages(sequenceMessages, true);
					}
				}
			}
		}

		public static void InsertOrUpdateTable(string queryInsert, string queryUpdate, params MySqlParameter[] parameters)
		{
			// Пробуем вставить строку в таблицу
			try
			{
				With.Connection(connection =>
				{
					MySqlHelper.ExecuteNonQuery(connection, queryInsert, parameters);
				});
			}
			catch (Exception)
			{
				// Если не получилось вставить строку, пробуем обновить ее
				With.Connection(connection =>
				{
					MySqlHelper.ExecuteNonQuery(connection, queryUpdate, parameters);
				});
			}
		}

		public static void RecreateDirectories()
		{
			if (Directory.Exists(Settings.Default.InboundPath))
				Directory.Delete(Settings.Default.InboundPath, true);
			if (Directory.Exists(Settings.Default.HistoryPath))
				Directory.Delete(Settings.Default.HistoryPath, true);
			if (Directory.Exists(Settings.Default.TempPath))
				Directory.Delete(Settings.Default.TempPath, true);
			Program.InitDirs(new[]
				         	{
				         		Settings.Default.BasePath,
				         		Settings.Default.ErrorFilesPath,
				         		Settings.Default.InboundPath,
				         		Settings.Default.TempPath,
				         		Settings.Default.HistoryPath
				         	});
		}
	}
}
