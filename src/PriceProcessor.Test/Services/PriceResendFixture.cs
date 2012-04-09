using System;
using System.Linq;
using Castle.ActiveRecord;
using NUnit.Framework;
using System.IO;
using System.ServiceModel;
using System.Net;
using Inforoom.PriceProcessor;
using PriceProcessor.Test.TestHelpers;
using RemotePriceProcessor;
using Test.Support;
using System.Collections.Generic;
using Test.Support.Catalog;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Services
{
	[TestFixture, Description("Тест для перепосылки прайса")]
	public class PriceResendFixture
	{
		private ServiceHost _serviceHost;
		private IRemotePriceProcessor priceProcessor;

		private string DataDirectory = @"..\..\Data";
		private TestSupplier supplier;
		private TestPriceSource source;
		private TestPriceItem priceItem;

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();
			_serviceHost = new ServiceHost(typeof(WCFPriceProcessorService));
			var binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			var url = String.Format("net.tcp://{0}:9847/RemotePriceProcessor", Dns.GetHostName());
			_serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessor), binding, url);
			_serviceHost.Open();
			var factory = new ChannelFactory<IRemotePriceProcessor>(binding, url);
			priceProcessor = factory.CreateChannel();

			supplier = TestSupplier.Create();

			var price = supplier.Prices[0];
			price.SetFormat(PriceFormatType.NativeDbf);
			priceItem = price.Costs[0].PriceItem;
			source = priceItem.Source;
			priceItem.Format.Save();
		}

		[TearDown]
		public void TearDown()
		{
			var communicationObject = ((ICommunicationObject)priceProcessor);
			if (communicationObject.State == CommunicationState.Faulted)
			{
				communicationObject.Abort();
			}
			else
			{
				communicationObject.Close();
			}
			_serviceHost.Close();
		}

		private void WcfCallResendPrice(uint downlogId)
		{
			WcfCall(r =>
			{
				var paramDownlogId = new WcfCallParameter
				{
					Value = downlogId,
					LogInformation = new LogInformation
					{
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};
				r.ResendPrice(paramDownlogId);
			});
		}

		private void WcfCall(Action<IRemotePriceProcessor> action)
		{
			action(priceProcessor);
		}

		[Test, Description("Тест для перепосылки прайса, присланного по email")]
		public void Resend_eml_price()
		{
			source.SourceType = PriceSourceType.Email;
			source.EmailFrom = "KvasovTest@analit.net";
			source.EmailTo = "KvasovTest@analit.net";
			source.PricePath = "price.zip";
			source.ExtrMask = "price.txt";
			source.Save();

			PriceDownloadLog downloadLog;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				downloadLog = new PriceDownloadLog {
					Addition = String.Empty,
					ArchFileName = "price.zip",
					ExtrFileName = "price.txt",
					Host = Environment.MachineName,
					LogTime = DateTime.Now,
					PriceItemId = priceItem.Id,
					ResultCode = 2
				};
				downloadLog.Save();
				scope.VoteCommit();
			}

			using (var sw = new FileStream(Path.Combine(Settings.Default.HistoryPath, downloadLog.Id + ".eml"), FileMode.CreateNew))
			{
				var attachments = new List<string> { Path.Combine(Path.GetFullPath(@"..\..\Data\"), "price.zip") };
				var message = ImapHelper.BuildMessageWithAttachments("KvasovTest@analit.net", "KvasovTest@analit.net", attachments.ToArray());
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

			source.PricePath  = archiveFileName;
			source.ExtrMask = externalFileName;
			source.Save();

			PriceDownloadLog downloadLog;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				downloadLog = new PriceDownloadLog {
					Addition = String.Empty,
					ArchFileName = archiveFileName,
					ExtrFileName = externalFileName,
					Host = Environment.MachineName,
					LogTime = DateTime.Now,
					PriceItemId = priceItem.Id,
					ResultCode = 2
				};
				downloadLog.Save();
				scope.VoteCommit();
			}
			var priceSrcPath = DataDirectory + Path.DirectorySeparatorChar + archiveFileName;
			var priceDestPath = Settings.Default.HistoryPath + Path.DirectorySeparatorChar + downloadLog.Id +
								Path.GetExtension(archiveFileName);
			File.Copy(priceSrcPath, priceDestPath, true);
			WcfCallResendPrice(downloadLog.Id);
			Assert.That(PriceItemList.list.FirstOrDefault(i => i.PriceItemId == priceItem.Id), Is.Not.Null, "Прайса нет в очереди на формализацию");
		}

		[Test, Description("Тест для перепосылки прайс-листа, который проверяет, правильный ли файл скопирован в DownHistory")]
		public void Copy_source_file_resend_price()
		{
			var sourceFileName = "6905885.eml";
			var archFileName = "сводныйпрайсч.rar"; //"prs.txt";
			var externalFileName = "сводныйпрайсч.txt";// archFileName;
			var email = "test@test.test";
			source.SourceType = PriceSourceType.Email;
			source.EmailFrom = email;
			source.EmailTo = email;
			source.PricePath = archFileName;
			source.ExtrMask = externalFileName;
			source.Save();

			PriceDownloadLog downloadLog;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				downloadLog = new PriceDownloadLog {
					Addition = String.Empty,
					ArchFileName = archFileName,
					ExtrFileName = externalFileName,
					Host = Environment.MachineName,
					LogTime = DateTime.Now,
					PriceItemId = priceItem.Id,
					ResultCode = 2
				};
				downloadLog.Save();
				scope.VoteCommit();
			}

			var priceSrcPath = DataDirectory + Path.DirectorySeparatorChar + sourceFileName;
			var priceDestPath = Settings.Default.HistoryPath + Path.DirectorySeparatorChar + downloadLog.Id +
								Path.GetExtension(sourceFileName);
			File.Copy(priceSrcPath, priceDestPath, true);
			WcfCallResendPrice(downloadLog.Id);

			var files = Directory.GetFiles(Settings.Default.HistoryPath);
			Assert.That(files.Length, Is.EqualTo(2));
			Assert.That(Path.GetExtension(files[0]), Is.EqualTo(Path.GetExtension(files[1])));
		}

		[Test]
		public void Smart_resend_should_resend_price_and_all_related_prices()
		{
			TestPrice rootPrice;
			TestPrice childPrice;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				rootPrice = supplier.Prices[0];
				rootPrice.SetFormat(PriceFormatType.NativeDbf);
				rootPrice.Save();
				scope.VoteCommit();
			}

			var supplier2 = TestSupplier.Create();
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				childPrice = supplier2.Prices[0];
				childPrice.SetFormat(PriceFormatType.NativeDbf);

				new TestUnrecExp("test", "test", childPrice).Save();
				new TestUnrecExp("test", "test", rootPrice).Save();
				childPrice.ParentSynonym = rootPrice.Id;
				childPrice.Save();
				scope.VoteCommit();
			}

			File.WriteAllBytes(Path.Combine(Settings.Default.BasePath, rootPrice.Costs[0].PriceItem.Id + ".dbf"), new byte[0]);
			File.WriteAllBytes(Path.Combine(Settings.Default.BasePath, childPrice.Costs[0].PriceItem.Id + ".dbf"), new byte[0]);

			WcfCall(r => r.RetransPriceSmart(childPrice.Id));

			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, rootPrice.Costs[0].PriceItem.Id + ".dbf")));
			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, childPrice.Costs[0].PriceItem.Id + ".dbf")));
		}
	}
}
