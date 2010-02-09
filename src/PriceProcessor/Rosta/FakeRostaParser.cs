using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Rosta
{
	public class RostaDecoder
	{
		public static string GetKey(string value)
		{
			var result = "";
			var key = 0x59;
			for(var i = 0; i < value.Length; i+=2)
			{
				var b = Convert.ToByte(value.Substring(i, 2), 16);
				var v = b ^ key;
				key = v;
				result += (char)v;
			}
			return result;
		}
	}

	public class FakeRostaParser : InterPriceParser
	{
		private string _producersFilename;

		public FakeRostaParser(string priceFileName, string groupsFileName, MySqlConnection connection, DataTable data)
			: base(priceFileName, connection, data)
		{
			_producersFilename = groupsFileName;
		}

		public override void Open()
		{
			convertedToANSI = true;
			var table = new DataTable();
			table.Columns.Add("Id", typeof (int));
			table.Columns.Add("ProductName", typeof (string));
			table.Columns.Add("ProducerName", typeof (string));
			table.Columns.Add("Quantity", typeof (int));
			table.Columns.Add("Cost", typeof (decimal));
			table.Columns.Add("Cost1", typeof (decimal));

			var producers = new List<string>();
			using(var stream = File.OpenRead(_producersFilename))
			{
				for(var i = 0; i < stream.Length / 41; i++)
				{
					var buffer = new byte[41];
					stream.Read(buffer, 0, buffer.Length);
					producers.Add(Encoding.GetEncoding(1251).GetString(buffer, 1, buffer[0]));
				}
			}

			using(var file = File.OpenRead(priceFileName))
			{
				var buffer = new byte[81];
				for(var i = 0; i < file.Length / 81; i++)
				{
					file.Read(buffer, 0, buffer.Length);
					var row = table.NewRow();
					row["Id"] = (int)BitConverter.ToUInt32(buffer, 0);
					row["Cost"] = (decimal)BitConverter.ToSingle(buffer, 32);
					row["Quantity"] = (int) BitConverter.ToUInt16(buffer, 6);
					row["ProductName"] = Encoding.GetEncoding(1251).GetString(buffer, 41, 40).Replace("\0", "");
					var producerIndex = BitConverter.ToUInt16(buffer, 4);
					row["ProducerName"] = producers[producerIndex];
					row["Cost1"] = BitConverter.ToSingle(buffer, 32);
					table.Rows.Add(row);
				}
			}

			table.Columns["Id"].ColumnName = "F1";
			table.Columns["ProductName"].ColumnName = "F2";
			table.Columns["ProducerName"].ColumnName = "F5";
			table.Columns["Quantity"].ColumnName = "F4";
			table.Columns["Cost"].ColumnName = "F3";
			table.Columns["Cost1"].ColumnName = "F7";
			dtPrice = table;
			CurrPos = 0;
			base.Open();
		}
	}
}