using System;
using System.Data;
using System.IO;
using Common.MySql;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using FileHelper = Common.Tools.FileHelper;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test.TestHelpers
{
	public class TestHelper
	{
		public static bool FulVerification;

		public static void InitDirs(params string[] dirs)
		{
			dirs.Each(dir => {
				if (Directory.Exists(dir)) {
					Directory.GetFiles(dir).Each(File.Delete);
					Directory.Delete(dir, true);
				}
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
			return With.Connection(c => {
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

		public static void Formalize(string file, int priceItemId)
		{
			var data = PricesValidator.LoadFormRules((uint)priceItemId);
			var typeName = String.Format("Inforoom.PriceProcessor.Formalizer.{0}, PriceProcessor", data.Rows[0]["ParserClassName"]);
			var parserType = Type.GetType(typeName);

			Formalize(parserType, data, file, priceItemId);
		}

		public static void Formalize(Type formatType, string file)
		{
			Formalize(formatType, file, Convert.ToInt32(Path.GetFileNameWithoutExtension(file)));
		}

		public static void Formalize<T>(string file, int priceItemId) where T : BasePriceParser
		{
			Formalize(typeof(T), file, priceItemId);
		}

		public static void Formalize(Type formatType, string file, int priceItemId)
		{
			Formalize(formatType, PricesValidator.LoadFormRules((uint)priceItemId), file, priceItemId);
		}

		public static void Formalize(Type formatType, DataTable parseRules, string file, int priceItemId)
		{
			using (var connection = new MySqlConnection(Literals.ConnectionString())) {
				var parser = (BasePriceParser)Activator.CreateInstance(formatType, file, connection, parseRules);
				parser.Formalize();
			}
		}

		public static void FormalizeOld(Type formatType, DataTable parseRules, string file, int priceItemId)
		{
			using (var connection = new MySqlConnection(Literals.ConnectionString())) {
				var parser = (BasePriceParser)Activator.CreateInstance(formatType, file, connection, parseRules);
				parser.Formalize();
			}
		}

		public static void Verify(string priceItemId)
		{
			var row = Fill(String.Format(@"
select pricecode, costcode
from usersettings.pricescosts
where priceitemid = {0}",
				priceItemId)).Tables[0].Rows[0];

			var pricecode = row[0];
			var costcode = row[1];

			var etalonCore0 = Fill(String.Format(@"
select c.*, cc.Cost
from core0_copy c
  join corecosts_copy cc on cc.Core_Id = c.Id
where c.pricecode = {0} and cc.pc_costcode = {1};",
				pricecode, costcode)).Tables[0];
			Verify(priceItemId, etalonCore0);
		}

		public static void Verify(string priceItemId, DataTable etalonCore0)
		{
			var row = Fill(String.Format(@"
select pricecode, costcode
from usersettings.pricescosts
where priceitemid = {0}",
				priceItemId)).Tables[0].Rows[0];

			var pricecode = row[0];
			var costcode = row[1];

			var resultCore0 = Fill(String.Format(@"
select c.*, cc.Cost, s.Synonym
from core0 c
  join corecosts cc on cc.Core_Id = c.Id
  join farm.Synonym s on s.SynonymCode = c.SynonymCode
where c.pricecode = {0} and cc.pc_costcode = {1} and c.synonymcode not in (4413102, 4413103);",
				pricecode, costcode)).Tables[0];

			Assert.That(resultCore0.Rows.Count, Is.GreaterThan(0), "������ �� �������������");
			Assert.That(resultCore0.Rows.Count, Is.EqualTo(etalonCore0.Rows.Count), "���������� ������� �� ���������");
			for (var i = 0; i < etalonCore0.Rows.Count; i++) {
				var etalonRow = etalonCore0.Rows[i];
				var resultRow = resultCore0.Rows[i];

				foreach (DataColumn column in etalonCore0.Columns) {
					var columnName = column.ColumnName.ToLower();
					if (columnName == "id" || columnName == "Synonym")
						continue;

					if (FulVerification) {
						if (!resultRow[column.ColumnName].Equals(etalonRow[column.ColumnName]))
							Console.WriteLine("�������� �� ��������� ������ {5} ��������� {6}. ������������ {4} ������ {3} ������� {0}. ������ ���������� {1}. ������ ������� {2}.",
								column.ColumnName,
								resultRow["Id"],
								etalonRow["Id"],
								i,
								resultRow["Synonym"],
								etalonRow[columnName],
								resultRow[columnName]);
					}
					else {
						Assert.That(resultRow[column.ColumnName],
							Is.EqualTo(etalonRow[column.ColumnName]),
							"������������ {4} ������ {3} ������� {0}. ������ ���������� {1}. ������ ������� {2}.", column.ColumnName,
							resultRow["Id"], etalonRow["Id"], i, resultRow["Synonym"]);
					}
				}
			}
		}

		public static void InsertOrUpdateTable(string queryInsert, string queryUpdate, params MySqlParameter[] parameters)
		{
			// ������� �������� ������ � �������
			try {
				With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, queryInsert, parameters); });
			}
			catch (Exception) {
				// ���� �� ���������� �������� ������, ������� �������� ��
				With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, queryUpdate, parameters); });
			}
		}

		public static void RecreateDirectories()
		{
			var dirs = new[] {
				Settings.Default.BasePath,
				Settings.Default.ErrorFilesPath,
				Settings.Default.InboundPath,
				Settings.Default.TempPath,
				Settings.Default.HistoryPath,
				Settings.Default.FTPOptBoxPath,
				Settings.Default.DownWaybillsPath,
				Settings.Default.DocumentPath,
				Settings.Default.CertificatePath,
				Settings.Default.AttachmentPath
			};

			dirs.Each(d => {
				if (Directory.Exists(d))
					FileHelper.DeleteDir(d);
				Directory.CreateDirectory(d);
			});
		}
	}
}