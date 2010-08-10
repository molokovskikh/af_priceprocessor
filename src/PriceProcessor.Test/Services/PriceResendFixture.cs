using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.ServiceModel;
using System.Net;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor;
using System.Net.Security;
using RemotePriceProcessor;
using Test.Support;
using System.Collections.Generic;

namespace PriceProcessor.Test.Services
{
	[TestFixture, Description("Тест для перепосылки прайса")]
	public class PriceResendFixture
	{
		private ServiceHost _serviceHost;

		private static readonly string DataDirectory = @"..\..\Data";

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();
			StartWcfPriceProcessor();
		}

		[TearDown]
		public void TearDown()
		{
			StopWcfPriceProcessor();
		}

				private void WcfCallResendPrice(ulong downlogId)
		{
			const string _strProtocol = @"net.tcp://";
			var binding = new NetTcpBinding();
			binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
			binding.Security.Mode = SecurityMode.None;
			// Ипользуется потоковая передача данных в обе стороны 
			binding.TransferMode = TransferMode.Streamed;
			// Максимальный размер принятых данных
			binding.MaxReceivedMessageSize = Int32.MaxValue;
			// Максимальный размер одного пакета
			binding.MaxBufferSize = 524288;    // 0.5 Мб
			var sbUrlService = new StringBuilder();
			sbUrlService.Append(_strProtocol)
				.Append(Dns.GetHostName()).Append(":")
				.Append(Settings.Default.WCFServicePort).Append("/")
				.Append(Settings.Default.WCFServiceName);
			var factory = new ChannelFactory<IRemotePriceProcessor>(binding, sbUrlService.ToString());
			var priceProcessor = factory.CreateChannel();
			var success = false;
			try
			{
				var paramDownlogId = new WcfCallParameter() {
					Value = downlogId,
					LogInformation = new LogInformation() {
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};
				priceProcessor.ResendPrice(paramDownlogId);
				((ICommunicationObject)priceProcessor).Close();
				success = true;
			}
			catch (FaultException)
			{
				if (((ICommunicationObject)priceProcessor).State != CommunicationState.Closed)
					((ICommunicationObject)priceProcessor).Abort();
				throw;
			}
			factory.Close();
			Assert.IsTrue(success);
		}

		private void StartWcfPriceProcessor()
		{
			const string _strProtocol = @"net.tcp://";
			var sbUrlService = new StringBuilder();
			_serviceHost = new ServiceHost(typeof(WCFPriceProcessorService));
			sbUrlService.Append(_strProtocol)
				.Append(Dns.GetHostName()).Append(":")
				.Append(Settings.Default.WCFServicePort).Append("/")
				.Append(Settings.Default.WCFServiceName);
			NetTcpBinding binding = new NetTcpBinding();
			binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
			binding.Security.Mode = SecurityMode.None;
			// Ипользуется потоковая передача данных в обе стороны 
			binding.TransferMode = TransferMode.Streamed;
			// Максимальный размер принятых данных
			binding.MaxReceivedMessageSize = Int32.MaxValue;
			// Максимальный размер одного пакета
			binding.MaxBufferSize = 524288;    // 0.5 Мб 
			_serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessor), binding,
				sbUrlService.ToString());
			_serviceHost.Open();
		}

		private void StopWcfPriceProcessor()
		{
			try
			{
				_serviceHost.Close();
			}
			catch { }
		}

		[Test, Description("Тест для перепосылки прайса, присланного по email")]
		public void Resend_eml_price()
		{
			var archiveFileName = @"price.zip";
			var externalFileName = @"price.txt";
			var password = "123";
			var emailFrom = "KvasovTest@analit.net";
			var emailTo = "KvasovTest@analit.net";
			var priceItem = TestPriceSource.CreateEmailPriceSource(emailFrom, emailTo, archiveFileName, externalFileName, password);

			var downloadLog = new PriceDownloadLog
			{
				Addition = String.Empty,
				ArchFileName = archiveFileName,
				ExtrFileName = externalFileName,
				Host = Environment.MachineName,
				LogTime = DateTime.Now,
				PriceItemId = priceItem.Id,
				ResultCode = 2
			};
			downloadLog.Save();
			
			using (var sw = new FileStream(Path.Combine(Settings.Default.HistoryPath, downloadLog.Id + ".eml"), FileMode.CreateNew))
			{
				var attachments = new List<string> {Path.Combine(Path.GetFullPath(@"..\..\Data\"), archiveFileName)};
				var message = TestHelper.BuildMessageWithAttachments(emailTo, emailFrom, attachments.ToArray());
				var bytes = message.ToByteData();
				sw.Write(bytes, 0, bytes.Length);
			}
			WcfCallResendPrice(downloadLog.Id);

			Assert.That(PriceItemList.list.FirstOrDefault(i => i.PriceItemId == priceItem.Id), Is.Not.Null, "Прайса нет в очереди на формализацию");
		}

		[Test, Description("Тест для перепосылки прайса, находящегося в папке и в архиве (когда разархивируем, получим папку, в которой лежит прайс)")]
		public void Resend_archive_with_files_in_folder()
		{
			var archiveFileName = @"price_in_dir.zip";
			var externalFileName = @"price.txt";

			var priceItem = TestPriceSource.CreateHttpPriceSource(archiveFileName, archiveFileName, externalFileName);
			var downloadLog = new PriceDownloadLog {
				Addition = String.Empty,
				ArchFileName = archiveFileName,
				ExtrFileName = externalFileName,
				Host = Environment.MachineName,
				LogTime = DateTime.Now,
				PriceItemId = priceItem.Id,
				ResultCode = 2
			};
			downloadLog.Save();
			var priceSrcPath = DataDirectory + Path.DirectorySeparatorChar + archiveFileName;
			var priceDestPath = Settings.Default.HistoryPath + Path.DirectorySeparatorChar + downloadLog.Id +
								Path.GetExtension(archiveFileName);
			File.Copy(priceSrcPath, priceDestPath, true);
			WcfCallResendPrice(downloadLog.Id);

			Assert.That(PriceItemList.list.FirstOrDefault(i => i.PriceItemId == priceItem.Id), Is.Not.Null, "Прайса нет в очереди на формализацию");
		}

		[Test, Description("Тест для перепосылки прайс-листа, который проверяет, правильный ли файл скопирован в DownHistory")]
		public void Test_copy_source_file_resend_price()
		{
			var sourceFileName = "6905885.eml";
			var archFileName = "сводныйпрайсч.rar"; //"prs.txt";
			var externalFileName = "сводныйпрайсч.txt";// archFileName;
			var email = "test@test.test";
			var priceItem = TestPriceSource.CreateEmailPriceSource(email, email, archFileName, externalFileName);
			var downloadLog = new PriceDownloadLog {
				Addition = String.Empty,
				ArchFileName = archFileName,
				ExtrFileName = externalFileName,
				Host = Environment.MachineName,
				LogTime = DateTime.Now,
				PriceItemId = priceItem.Id,
				ResultCode = 2
			};
			downloadLog.Save();
			var priceSrcPath = DataDirectory + Path.DirectorySeparatorChar + sourceFileName;
			var priceDestPath = Settings.Default.HistoryPath + Path.DirectorySeparatorChar + downloadLog.Id +
								Path.GetExtension(sourceFileName);
			File.Copy(priceSrcPath, priceDestPath, true);
			WcfCallResendPrice(downloadLog.Id);

			var files = Directory.GetFiles(Settings.Default.HistoryPath);
			Assert.That(files.Length, Is.EqualTo(2));
			Assert.That(Path.GetExtension(files[0]), Is.EqualTo(Path.GetExtension(files[1])));
		}
	}
}
