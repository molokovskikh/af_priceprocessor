using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common.Tools.Calendar;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using log4net;

namespace Inforoom.PriceProcessor.Rosta
{
	public interface IDownloader
	{
		void DownloadPrice(string key, string hwinfo, string price, string producers, string ex);
	}

	public class RostaDownloader : IDownloader
	{
		private readonly ILog _logger = LogManager.GetLogger(typeof (RostaDownloader));

		private NetworkStream stream;
		private StreamWriter writer;
		private StreamReader reader;
		private Encoding encoding = Encoding.GetEncoding(1251);
		private string priceFile;
		private string producersFile;

		public void DownloadPrice(string key, string hwinfo,  string price, string producers, string ex)
		{
			priceFile = price;
			producersFile = producers;
			using( var tcpClient = new TcpClient())
			{
				tcpClient.SendTimeout = (int) 60.Second().TotalMilliseconds;
				tcpClient.ReceiveTimeout = (int) 60.Second().TotalMilliseconds;
				tcpClient.Connect(new IPEndPoint(IPAddress.Parse("77.233.165.8"), 215));
				using(stream = tcpClient.GetStream())
				using(writer = new StreamWriter(stream, encoding, 1))
				using(reader = new StreamReader(stream, encoding, false, 1))
				{
					writer.AutoFlush = true;
					var responce = ReadResponce(String.Format("LOGIN {0} 05010130", key));
					var parts = responce.Split(' ');
					if (parts.Length >= 5)
					{
						var serverKey = Convert.ToInt32(parts[4].Trim());
						ReadResponce("HWINFO {0}", RostaDecoder.CryptHwinfo(hwinfo, serverKey));
					}
					ReadResponce("SET CODEPAGE WIN1251");
					ReadResponce("GET EXPORT_PERMISSION");
					RequestAndDownload("GETZ PRICELISTS");
					RequestAndDownload("GETZ GROUPS", producers + "_source");
					RequestAndDownload("GETZ PRICES 0", price + "_source");
					RequestAndDownload("GETZ PRICES 1", price + "_source");
					RequestAndDownload("GETZ PRICES 2", price + "_source");
					RequestAndDownload("GETZ PRICES 3", price + "_source");
					RequestAndDownload("GETZ PRICES 4", price + "_source");
					RequestAndDownload("GETZ PRICES 5", price + "_source");
					RequestAndDownload("GETZ PRICES 6", price + "_source");
					RequestAndDownload("GETZ PRICES 7", price + "_source");
					RequestAndDownload("GETZ PRICES 8", price + "_source");
					RequestAndDownload("GETZ DOC_TYPES");
					RequestAndDownload("GETZ COLUMNS FORCESHOW FULLNAME PACKSTOCK PREDELPRICE ZNVLS ARTICULSID ARTICULSGROUP ABATEDATE MULTIPLY");
					RequestAndDownload("GETZ EXTENDED FORCESHOW FULLNAME PACKSTOCK PREDELPRICE ZNVLS ARTICULSID ARTICULSGROUP ABATEDATE MULTIPLY", ex + "_source");
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

			Unpack(new[] {
				producersFile + "_source",
				priceFile + "_source",
				ex + "_source"
			});
		}

		private string ReadResponce(string request, params object[] args)
		{
			return ReadResponce(String.Format(request, args), null, true);
		}

		private string ReadResponce(string request, string  fileToDownload, bool checkResponce)
		{
			if (_logger.IsDebugEnabled)
				_logger.DebugFormat(request);
			writer.WriteLine(request);

			var responce = reader.ReadLine();
			if (_logger.IsDebugEnabled)
				_logger.Debug(responce);

			var size = GetDataSize(responce);
			if (size > 0)
			{
				if (String.IsNullOrEmpty(fileToDownload))
				{
					Download(stream, Stream.Null, size);
				}
				else
				{
					using(var fileStream = File.Create(fileToDownload))
						Download(stream, fileStream, size);
				}
			}
			if (responce.StartsWith("DATASIZE"))
			{
				var line = reader.ReadLine();
				if (_logger.IsDebugEnabled)
					_logger.Debug(line);
			}
			else if (checkResponce && !responce.StartsWith("OK"))
				throw new Exception(String.Format("Неизвестный ответ сервера {0} на запрос {1}", responce, request));
			return responce;
		}

		private void RequestAndDownload(string request)
		{
			ReadResponce(request, null, false);
		}

		private void RequestAndDownload(string request, string downloadToFile)
		{
			ReadResponce(request, downloadToFile, false);
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

		public static void Unpack(string[] files)
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