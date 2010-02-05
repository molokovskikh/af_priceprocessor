using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Inforoom.Formalizer;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class RostaLoader
	{
		private ILog _logger = LogManager.GetLogger(typeof (RostaLoader));

		private NetworkStream stream;
		private StreamWriter writer;
		private StreamReader reader;
		private Encoding encoding = Encoding.GetEncoding(1251);
		private string key;
		private string priceFile;
		private string producersFile;

		public RostaLoader(string key)
		{
			this.key = key;
		}

		public void DownloadPrice(string price, string producers)
		{
			priceFile = price;
			producersFile = producers;
			using( var tcpClient = new TcpClient())
			{
				tcpClient.Connect(new IPEndPoint(IPAddress.Parse("77.233.165.8"), 215));
				using(stream = tcpClient.GetStream())
				using(writer = new StreamWriter(stream, encoding, 1))
				using(reader = new StreamReader(stream, encoding, false, 1))
				{
					writer.AutoFlush = true;
					writer.WriteLine("LOGIN {0}", key);
					ReadResponce();
					writer.WriteLine("SET CODEPAGE WIN1251");
					ReadResponce();
					writer.WriteLine("GET EXPORT_PERMISSION");
					ReadResponce();
					RequestAndDownload("GETZ PRICELISTS");
					RequestAndDownload("GETZ GROUPS");
					RequestAndDownload("GETZ PRICES 0");
					RequestAndDownload("GETZ PRICES 1");
					RequestAndDownload("GETZ PRICES 2");
					RequestAndDownload("GETZ PRICES 3");
					RequestAndDownload("GETZ PRICES 4");
					RequestAndDownload("GETZ PRICES 5");
					RequestAndDownload("GETZ PRICES 5");
					RequestAndDownload("GETZ PRICES 6");
					RequestAndDownload("GETZ PRICES 7");
					RequestAndDownload("GETZ PRICES 8");
					RequestAndDownload("GETZ DOC_TYPES");
					//RequestAndDownload("GETZ COLUMNS FORCESHOW FULLNAME PACKSTOCK REESTRPRICE ARTIKULSID");
					RequestAndDownload("GETZ COLUMNS FORCESHOW");
					RequestAndDownload("GETZ EXTENDED FORCESHOW");
					RequestAndDownload("GETZ CATEGORIES");
					RequestAndDownload("GETZ CERTIFICATES");
					RequestAndDownload("GETZ REPORT_LIST");
					RequestAndDownload("GETZ PARTNERADDRESSES");
					RequestAndDownload("GETZ MESSAGE 928668224 0");
					RequestAndDownload("GETZ LAST_UPDATES WIN 05010130");
					RequestAndDownload("GETZ NEWS 05010130");
					RequestAndDownload("GETZ ICONS");
					RequestAndDownload("LOGOUT");
				}
			}

			Decode();
		}

		private void ReadResponce()
		{
			SoftReader(null);
		}

		private void ReadResponce(string request)
		{
			SoftReader(request);
		}

		private void RequestAndDownload(string request)
		{
			if (_logger.IsDebugEnabled)
			{
				Console.WriteLine(request);
				_logger.Debug(request);
			}
			writer.WriteLine(request);
			ReadResponce(request);
		}

		private void SoftReader(string request)
		{
			var header = reader.ReadLine();
			if (_logger.IsDebugEnabled)
			{
				Console.WriteLine(header);
				_logger.Debug(header);
			}

			var size = GetDataSize(header);
			if (size > 0)
			{
				var path = "";
				if (request.StartsWith("GETZ PRICES"))
					path = priceFile + "_source";
				else if (request.StartsWith("GETZ GROUPS"))
					path = producersFile + "_source";

				if (!String.IsNullOrEmpty(path))
				{
					using (var write = File.OpenWrite(path))
						Download(stream, write, size);
				}
				else
					Download(stream, Stream.Null, size);
			}
			if (header.StartsWith("DATASIZE"))
			{
				if (_logger.IsDebugEnabled)
				{
					var line = reader.ReadLine();
					Console.WriteLine(line);
					_logger.Debug(line);
				}
			}
		}

		private int GetDataSize(string response)
		{
			if (response.StartsWith("DATASIZE"))
				return  Convert.ToInt32(response.Replace("DATASIZE", "").Trim());
			return 0;
		}

		private void Download(Stream from, Stream to, int size)
		{
			if (size == 0)
				return;

			var buffer = new byte[size];
			do
			{
				var readed = from.Read(buffer, 0, size);
				to.Write(buffer, 0, readed);
				size = size - readed;
			} while (size > 0);
		}

		private void Decode()
		{
			foreach (var file in new [] {producersFile + "_source", priceFile + "_source"})
			{
				using(var input = File.OpenRead(file))
				using(var output = File.OpenWrite(file.Replace("_source", "")))
				{
					var inflater = new InflaterInputStream(input);
					var buffer = new byte[10*1024];
					int readed;
					do
					{
						readed = inflater.Read(buffer, 0, buffer.Length);
						output.Write(buffer, 0, readed);
					} while (readed > 0);
				}
			}
		}

	}

	public class RostaDecoder
	{
		public static string GetKey(string value)
		{
			var result = "";
			var key = 0x59;
			//var value = "6B02010100010300030607050504191807011E1C03";
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

	//Прайс
	//всего 81 байт
	//4 байта - id|2 байта - нидекс производителя|2 или 4 байта - количество|
	//4 байта - идентификатор группы(гомеопатия, химия и тд)|
	//20 или 18 байта - неизвестно
	//4 байта - цена(Предоплатная цена)|4 байта - еще цена(Базовая цена)|1 байт - неизвестно|
	//41 байт - Наименование
	//Производители
	//Просто массив записей. Длинна записи 41 байт.
	//1 байт - Длинна строки | дальше строка
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
