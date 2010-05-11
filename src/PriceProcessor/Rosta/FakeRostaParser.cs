﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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

		public static string CryptHwinfo(string hwinfo, int seed)
		{
			var bytes = Encoding.ASCII.GetBytes(hwinfo);

			Seed = seed;
			var randomTailLenght = Next(20) + 10;
			var newBytes = new byte[bytes.Length + randomTailLenght + 1];
			Array.Copy(bytes, newBytes, bytes.Length);

			bytes = newBytes;

			for (var i = bytes.Length  - randomTailLenght; i < bytes.Length; i++)
				bytes[i] = (byte) (Next(64) + 48);

			//в алгоритме похоже есть ошибка мы не учитываем последний байт
			var cryptLength = bytes.Length - 1;
			for(var i = 0; i < cryptLength; i++)
				bytes[i] ^= (byte) (Math.Round(Math.Sin(i)*128, 0) - cryptLength);

			CalculateCheckSum(bytes, cryptLength);

			for(var i = 0; i < cryptLength; i++)
			{
				if (i != cryptLength - 4)
					bytes[i] ^= bytes[cryptLength - 4];
			}

			var result = "";
			for(var i = 0; i < cryptLength; i++)
				result += Convert.ToString(bytes[i], 16).ToUpper().PadLeft(2, '0');
			return result;
		}

		private static void CalculateCheckSum(byte[] bytes, int cryptLength)
		{
			byte checkSum = 0;
			for(var i = 0; i < cryptLength; i++)
				checkSum = (byte) (bytes[i] + (byte)(checkSum << 7 | checkSum >> 1));
			bytes[cryptLength - 1] = (byte) (bytes[cryptLength - 1] - checkSum - 127);
		}

		private static int Seed;

		public static int Next(int max)
		{
			Seed = Seed*0x8088405;
			Seed++;
			var r = ((ulong)((uint)Seed) * (ulong) max) >> 32;
			return (int) r;
		}
	}

	public class FakeRostaParser : InterPriceParser
	{
		private string _producersFilename;
		private string _addtionDataFileName;

		public FakeRostaParser(string priceFileName, string groupsFileName, string addtionDataFileName, MySqlConnection connection, DataTable data)
			: base(priceFileName, connection, data)
		{
			_producersFilename = groupsFileName;
			_addtionDataFileName = addtionDataFileName;
		}

		public override void Open()
		{
			convertedToANSI = true;
			var table = new DataTable();
			table.Columns.Add("Id", typeof (int));
			table.Columns.Add("ProductName", typeof (string));
			table.Columns.Add("ProducerName", typeof (string));
			table.Columns.Add("Quantity", typeof (int));
			table.Columns.Add("Period", typeof (DateTime));
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

			var additions = RostaReader.ReadAddtions(_addtionDataFileName);

			using(var file = File.OpenRead(priceFileName))
			{
				var buffer = new byte[81];
				for(var i = 0; i < file.Length / 81; i++)
				{
					file.Read(buffer, 0, buffer.Length);
					var row = table.NewRow();
					var id = (int)BitConverter.ToUInt32(buffer, 0);
					row["Id"] = id;
					var producerIndex = BitConverter.ToUInt16(buffer, 4);
					row["Quantity"] = (int) BitConverter.ToUInt16(buffer, 6);
					row["Cost"] = (decimal)BitConverter.ToSingle(buffer, 32);
					row["Cost1"] = BitConverter.ToSingle(buffer, 32);
					row["ProductName"] = Encoding.GetEncoding(1251).GetString(buffer, 41, 40).Replace("\0", "");
					row["ProducerName"] = producers[producerIndex];
					var addtionData = additions.FirstOrDefault(a => a.Id == id);
					if (addtionData != null)
						row["Period"] = addtionData.Period;
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

	public class AditionData
	{
		public uint Id;
		public DateTime Period;
		public uint Pack;
		public bool VitallyImportant;
		public float RegistryCost;
	}


	public class RostaReader
	{
		//длинна 244 байта
		//4 байта - код из прайса
		//80 байт (возможно меньше) - наименование
		//c 84, 4 байта - uint количество в упаковке
		//c 88, 4 байта - float предельная зарегистрированная цена
		//c 92, 5 байт может больше - строка, ЖНВЛС
		//c 232, 8 байт - double в виде TDateTime
		public static List<AditionData> ReadAddtions(string s)
		{
			var result = new List<AditionData>();
			using(var file = File.OpenRead(s))
			{
				var buffer = new byte[244];
				while(file.Read(buffer, 0, buffer.Length) > 0)
				{
					var id = BitConverter.ToUInt32(buffer, 0);
					//var name = Encoding.GetEncoding(1251).GetString(buffer, 4, 80);
					var date = BitConverter.ToDouble(buffer, 232);
					result.Add(new AditionData {
						Id = id,
						Period = new DateTime(1899, 12, 30) + TimeSpan.FromDays(date),
						Pack = BitConverter.ToUInt32(buffer, 84),
						RegistryCost = BitConverter.ToSingle(buffer, 88),
						VitallyImportant = Encoding.GetEncoding(1251).GetString(buffer, 92, 5) == "ЖНВЛС"
					});
				}
			}
			return result;
		}
	}
}