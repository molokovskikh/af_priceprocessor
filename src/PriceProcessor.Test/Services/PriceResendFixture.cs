﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Wcf;
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
using Monitor = Inforoom.PriceProcessor.Monitor;

namespace PriceProcessor.Test.Services
{
	[TestFixture, Description("Тест для перепосылки прайса")]
	public class PriceResendFixture
	{
		private ServiceHost host;
		private ChannelFactory<IRemotePriceProcessor> factory;

		private IRemotePriceProcessor priceProcessor;

		private string DataDirectory = @"..\..\Data";
		private TestSupplier supplier;
		private TestPriceSource source;
		private TestPriceItem priceItem;

		private TestPrice rootPrice;
		private TestPrice childPrice;
		private string url;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			url = "net.tcp://localhost:9847/RemotePriceProcessor";
			host = Monitor.StartService(typeof(IRemotePriceProcessor),
				typeof(WCFPriceProcessorService),
				url,
				Settings.Default.WCFQueueName);
			factory = new ChannelFactory<IRemotePriceProcessor>(host.Description.Endpoints[0].Binding, url);
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			host.Close();
		}

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();
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
			if (communicationObject.State == CommunicationState.Faulted) {
				communicationObject.Abort();
			}
			else {
				communicationObject.Close();
			}
		}

		private void WcfCallResendPrice(uint downlogId)
		{
			var paramDownlogId = new WcfCallParameter {
				Value = downlogId,
				LogInformation = new LogInformation {
					ComputerName = Environment.MachineName,
					UserName = Environment.UserName
				}
			};
			priceProcessor.ResendPrice(paramDownlogId);
		}

		private void CreatePrices()
		{
			using (var scope = new TransactionScope(OnDispose.Rollback)) {
				rootPrice = supplier.Prices[0];
				rootPrice.SetFormat(PriceFormatType.NativeDbf);
				rootPrice.Save();
				scope.VoteCommit();
			}

			var supplier2 = TestSupplier.Create();
			using (var scope = new TransactionScope(OnDispose.Rollback)) {
				childPrice = supplier2.Prices[0];
				childPrice.SetFormat(PriceFormatType.NativeDbf);

				new TestUnrecExp("test", "test", childPrice).Save();
				new TestUnrecExp("test", "test", rootPrice).Save();
				childPrice.ParentSynonym = rootPrice.Id;
				childPrice.Save();
				scope.VoteCommit();
			}
		}

		[Test, Description("Тест для перепосылки прайса, присланного по email")]
		public void Resend_eml_price()
		{
			var file = "price1.zip";

			source.SourceType = PriceSourceType.Email;
			source.EmailFrom = "KvasovTest@analit.net";
			source.EmailTo = "KvasovTest@analit.net";
			source.PricePath = file;
			source.ExtrMask = "price.txt";
			source.Save();

			PriceDownloadLog downloadLog;
			using (var scope = new TransactionScope(OnDispose.Rollback)) {
				downloadLog = new PriceDownloadLog {
					Addition = String.Empty,
					ArchFileName = file,
					ExtrFileName = "price.txt",
					Host = Environment.MachineName,
					LogTime = DateTime.Now,
					PriceItemId = priceItem.Id,
					ResultCode = 2
				};
				downloadLog.Save();
				scope.VoteCommit();
			}

			using (var sw = new FileStream(Path.Combine(Settings.Default.HistoryPath, downloadLog.Id + ".eml"), FileMode.CreateNew)) {
				var attachments = new List<string> { Path.Combine(DataDirectory, file) };
				var message = ImapHelper.BuildMessageWithAttachments("KvasovTest@analit.net", "KvasovTest@analit.net", attachments.ToArray());
				var bytes = message.ToByteData();
				sw.Write(bytes, 0, bytes.Length);
			}
			WcfCallResendPrice(downloadLog.Id);

			Assert.That(PriceItemList.list.FirstOrDefault(i => i.PriceItemId == priceItem.Id), Is.Not.Null, "Прайса нет в очереди на формализацию");
			Assert.IsFalse(File.Exists(Path.Combine(Path.GetTempPath(), file)), "Не удален временный файл из темп");
		}

		[Test, Description("Тест для перепосылки прайса, находящегося в папке и в архиве (когда разархивируем, получим папку, в которой лежит прайс)")]
		public void Resend_archive_with_files_in_folder()
		{
			var file = "price_in_dir.zip";
			var priceFile = "price.txt";

			source.SourceType = PriceSourceType.Lan;
			source.PricePath = file;
			source.ExtrMask = priceFile;
			source.Save();

			PriceDownloadLog downloadLog;
			using (var scope = new TransactionScope(OnDispose.Rollback)) {
				downloadLog = new PriceDownloadLog {
					Addition = String.Empty,
					ArchFileName = file,
					ExtrFileName = priceFile,
					Host = Environment.MachineName,
					LogTime = DateTime.Now,
					PriceItemId = priceItem.Id,
					ResultCode = 2
				};
				downloadLog.Save();
				scope.VoteCommit();
			}
			var priceSrcPath = Path.Combine(DataDirectory, file);
			var priceDestPath = Path.Combine(Settings.Default.HistoryPath, downloadLog.Id + Path.GetExtension(file));
			File.Copy(priceSrcPath, priceDestPath, true);
			WcfCallResendPrice(downloadLog.Id);
			Assert.That(PriceItemList.list.FirstOrDefault(i => i.PriceItemId == priceItem.Id), Is.Not.Null, "Прайса нет в очереди на формализацию");
		}

		[Test, Description("Тест для перепосылки прайс-листа, который проверяет, правильный ли файл скопирован в DownHistory")]
		public void Copy_source_file_resend_price()
		{
			var sourceFileName = "6905885.eml";
			var archFileName = "сводныйпрайсч.rar"; //"prs.txt";
			var externalFileName = "сводныйпрайсч.txt"; // archFileName;
			var email = "test@test.test";
			source.SourceType = PriceSourceType.Email;
			source.EmailFrom = email;
			source.EmailTo = email;
			source.PricePath = archFileName;
			source.ExtrMask = externalFileName;
			source.Save();

			PriceDownloadLog downloadLog;
			using (var scope = new TransactionScope(OnDispose.Rollback)) {
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
		public void RetransErrorPriceTest()
		{
			File.WriteAllBytes(Path.Combine(Settings.Default.ErrorFilesPath, priceItem.Id + ".dbf"), new byte[0]);
			priceProcessor.RetransErrorPrice(new WcfCallParameter {
				LogInformation = new LogInformation {
					ComputerName = "test",
					UserName = "test"
				},
				Value = priceItem.Id
			});
			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, priceItem.Id + ".dbf")));
			Assert.That(!File.Exists(Path.Combine(Settings.Default.ErrorFilesPath, priceItem.Id + ".dbf")));
		}

		[Test]
		public void RetransPriceTest()
		{
			File.WriteAllBytes(Path.Combine(Settings.Default.BasePath, priceItem.Id + ".dbf"), new byte[0]);
			priceProcessor.RetransPrice(new WcfCallParameter {
				LogInformation = new LogInformation {
					ComputerName = "test",
					UserName = "test"
				},
				Value = priceItem.Id
			});
			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, priceItem.Id + ".dbf")));
			Assert.That(File.Exists(Path.Combine(Settings.Default.BasePath, priceItem.Id + ".dbf")));
		}

		[Test]
		public void NotRetransIfExistsItem()
		{
			var path = Path.Combine(Settings.Default.BasePath, priceItem.Id + ".dbf");
			PriceItemList.AddItem(new PriceProcessItem(false,
				supplier.Prices[0].Id,
				supplier.Prices[0].Costs[0].Id,
				priceItem.Id,
				path,
				null));
			File.WriteAllBytes(path, new byte[0]);
			var ex = Assert.Throws<FaultException>(() => priceProcessor.RetransPrice(new WcfCallParameter {
				LogInformation = new LogInformation {
					ComputerName = "test",
					UserName = "test"
				},
				Value = priceItem.Id
			}));
			Assert.That(ex.Message, Is.EqualTo("Данный прайс-лист находится в очереди на формализацию"));
		}

		[Test]
		public void Smart_resend_should_resend_price_and_all_related_prices()
		{
			CreatePrices();

			File.WriteAllBytes(Path.Combine(Settings.Default.BasePath, rootPrice.Costs[0].PriceItem.Id + ".dbf"), new byte[0]);
			File.WriteAllBytes(Path.Combine(Settings.Default.BasePath, childPrice.Costs[0].PriceItem.Id + ".dbf"), new byte[0]);

			priceProcessor.RetransPriceSmart(childPrice.Id);

			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, rootPrice.Costs[0].PriceItem.Id + ".dbf")));
			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, childPrice.Costs[0].PriceItem.Id + ".dbf")));

			Assert.That(File.Exists(Path.Combine(Settings.Default.BasePath, rootPrice.Costs[0].PriceItem.Id + ".dbf")));
			Assert.That(File.Exists(Path.Combine(Settings.Default.BasePath, childPrice.Costs[0].PriceItem.Id + ".dbf")));
		}

		[Test]
		public void Msmq_test_retrans_price()
		{
			CreatePrices();
			var priceProcessor = new PriceProcessorWcfHelper(url, Settings.Default.WCFQueueName);

			File.WriteAllBytes(Path.Combine(Settings.Default.BasePath, rootPrice.Costs[0].PriceItem.Id + ".dbf"), new byte[0]);
			File.WriteAllBytes(Path.Combine(Settings.Default.BasePath, childPrice.Costs[0].PriceItem.Id + ".dbf"), new byte[0]);

			priceProcessor.RetransPrice(rootPrice.Costs[0].PriceItem.Id, true);
			priceProcessor.RetransPrice(childPrice.Costs[0].PriceItem.Id, true);

			Thread.Sleep(1000);

			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, rootPrice.Costs[0].PriceItem.Id + ".dbf")));
			Assert.That(File.Exists(Path.Combine(Settings.Default.InboundPath, childPrice.Costs[0].PriceItem.Id + ".dbf")));
		}
	}
}
