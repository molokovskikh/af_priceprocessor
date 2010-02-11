using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using log4net;

namespace Inforoom.PriceProcessor.Rosta
{
	public interface IDownloader
	{
		void DownloadPrice(string key, string price, string producers, string ex);
	}

	public class RostaDownloader : IDownloader
	{
		private readonly ILog _logger = LogManager.GetLogger(typeof (RostaDownloader));

		private NetworkStream stream;
		private StreamWriter writer;
		private StreamReader reader;
		private Encoding encoding = Encoding.GetEncoding(1251);
		private string key;
		private string priceFile;
		private string producersFile;

		public void DownloadPrice(string key, string price, string producers, string ex)
		{
			this.key = key;
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
					RequestAndDownload("GETZ PRICES 0", producers + "_source");
					RequestAndDownload("GETZ PRICES 1", price + "_source");
					RequestAndDownload("GETZ PRICES 2", price + "_source");
					RequestAndDownload("GETZ PRICES 3", price + "_source");
					RequestAndDownload("GETZ PRICES 4", price + "_source");
					RequestAndDownload("GETZ PRICES 5", price + "_source");
					RequestAndDownload("GETZ PRICES 6", price + "_source");
					RequestAndDownload("GETZ PRICES 7", price + "_source");
					RequestAndDownload("GETZ PRICES 8", price + "_source");
					RequestAndDownload("GETZ DOC_TYPES");
					RequestAndDownload("GETZ COLUMNS FORCESHOW FULLNAME PACKSTOCK REESTRPRICE ARTIKULSID ARTIKULSGROUP ABATEDATE MULTIPLY");
					RequestAndDownload("GETZ EXTENDED FORCESHOW FULLNAME PACKSTOCK REESTRPRICE ARTIKULSID ARTIKULSGROUP ABATEDATE MULTIPLY", ex + "_source");
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

			Decode(new[] {
				producersFile + "_source",
				priceFile + "_source",
				ex + "_source"
			});
		}

		private void ReadResponce()
		{
			ReadResponce(null);
		}

		private void ReadResponce(string downlodToFile)
		{
			var header = reader.ReadLine();
			if (_logger.IsDebugEnabled)
			{
				_logger.Debug(header);
			}

			var size = GetDataSize(header);
			if (size > 0)
			{
				if (String.IsNullOrEmpty(downlodToFile))
				{
					Download(stream, Stream.Null, size);
				}
				else
				{
					using(var fileStream = File.Create(downlodToFile))
						Download(stream, fileStream, size);
				}
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

		private void RequestAndDownload(string request)
		{
			if (_logger.IsDebugEnabled)
				_logger.Debug(request);
			writer.WriteLine(request);
			ReadResponce(request);
		}

		private void RequestAndDownload(string request, string downloadToFile)
		{
			if (_logger.IsDebugEnabled)
				_logger.Debug(request);
			writer.WriteLine(request);
			ReadResponce(downloadToFile);
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

		public static void Decode(string[] files)
		{
			
			foreach (var file in files)
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
}