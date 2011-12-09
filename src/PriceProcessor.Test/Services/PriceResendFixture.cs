﻿using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using NUnit.Framework;
using System.IO;
using System.ServiceModel;
using System.Net;
using Inforoom.PriceProcessor;
using PriceProcessor.Test.Handlers;
using PriceProcessor.Test.TestHelpers;
using RemotePriceProcessor;
using Test.Support;
using System.Collections.Generic;
using Test.Support.Catalog;
using Test.Support.Suppliers;
using FileHelper = Inforoom.Common.FileHelper;

namespace PriceProcessor.Test.Services
{
	[TestFixture, Description("Тест для перепосылки прайса")]
	public class PriceResendFixture
	{
		private ServiceHost _serviceHost;
		private IRemotePriceProcessor priceProcessor;

		private static readonly string DataDirectory = @"..\..\Data";

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
		}

		[TearDown]
		public void TearDown()
		{
			Assert.That(((ICommunicationObject)priceProcessor).State, Is.EqualTo(CommunicationState.Opened));
			((ICommunicationObject)priceProcessor).Close();
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

		private string[] WcfCall(Func<IRemotePriceProcessor, string[]> action)
		{
			string[] res = new string[0];
			res = action(priceProcessor);
			return res;
		}

		[Test]
		[Ignore("Для ручного тестирования")]
		public void FindSynonymTest()
		{
			TestPrice price;
			TestPriceItem priceItem;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				price = TestSupplier.CreateTestSupplierWithPrice(p =>
				{
					var rules = p.Costs.Single().PriceItem.Format;
					rules.PriceFormat = PriceFormatType.NativeDelimiter1251;
					rules.Delimiter = ";";
					rules.FName1 = "F2";
					rules.FFirmCr = "F3";
					rules.FQuantity = "F5";
					p.Costs.Single().FormRule.FieldName = "F4";
					rules.FRequestRatio = "F6";
					p.ParentSynonym = 5;
				});
				priceItem = price.Costs.First().PriceItem;
				scope.VoteCommit();
			}
			string basepath = FileHelper.NormalizeDir(Settings.Default.BasePath);
			if (!Directory.Exists(basepath)) Directory.CreateDirectory(basepath);

			File.Copy(Path.GetFullPath(@"..\..\Data\223.txt"), Path.GetFullPath(@"..\..\Data\2223.txt"));
			File.Move(Path.GetFullPath(@"..\..\Data\2223.txt"), Path.GetFullPath(String.Format(@"{0}{1}.txt", basepath, priceItem.Id)));

			PriceProcessItem item = PriceProcessItem.GetProcessItem(priceItem.Id);
			var names = item.GetAllNames();
			Assert.That(names.Count(), Is.EqualTo(5));

			TestIndexerHandler handler = new TestIndexerHandler();
			handler.DoIndex();
			var res1 = WcfCall(r =>
			{
				return r.FindSynonyms(priceItem.Id);
			});


			Assert.That(res1.Length, Is.EqualTo(2));
			Assert.That(res1[0], Is.EqualTo("Success"));

			long taskId = Convert.ToInt64(res1[1]);


			Thread.Sleep(5000);

			var res2 = WcfCall(r =>
			{
				return r.FindSynonymsResult(taskId.ToString());
			}
				);

			File.Delete(Path.GetFullPath(String.Format(@"{0}{1}.txt", basepath, priceItem.Id)));
		}


		[Test, Description("Тест для перепосылки прайса, присланного по email")]
		public void Resend_eml_price()
		{
			var archiveFileName = @"price.zip";
			var externalFileName = @"price.txt";
			var password = "123";
			var emailFrom = "KvasovTest@analit.net";
			var emailTo = "KvasovTest@analit.net";
			PriceDownloadLog downloadLog;

			TestPriceItem priceItem;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				priceItem = TestPriceSource.CreateEmailPriceSource(emailFrom, emailTo, archiveFileName,
																	   externalFileName, password);
				scope.VoteCommit();
			}
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var priceCost = TestPriceCost.Queryable.Where(c => c.PriceItem.Id == priceItem.Id).FirstOrDefault();
				priceCost.BaseCost = true;
				priceCost.Save();
				downloadLog = new PriceDownloadLog
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
				scope.VoteCommit();
			}



			using (var sw = new FileStream(Path.Combine(Settings.Default.HistoryPath, downloadLog.Id + ".eml"), FileMode.CreateNew))
			{
				var attachments = new List<string> { Path.Combine(Path.GetFullPath(@"..\..\Data\"), archiveFileName) };
				var message = ImapHelper.BuildMessageWithAttachments(emailTo, emailFrom, attachments.ToArray());
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

			PriceDownloadLog downloadLog;
			TestPriceItem priceItem;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				priceItem = TestPriceSource.CreateHttpPriceSource(archiveFileName, archiveFileName, externalFileName);
				scope.VoteCommit();
			}

			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var priceCost = TestPriceCost.Queryable.Where(c => c.PriceItem.Id == priceItem.Id).FirstOrDefault();
				priceCost.BaseCost = true;
				priceCost.Save();
				downloadLog = new PriceDownloadLog
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
			PriceDownloadLog downloadLog;
			TestPriceItem priceItem;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				priceItem = TestPriceSource.CreateEmailPriceSource(email, email, archFileName, externalFileName);
				scope.VoteCommit();
			}

			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				var priceCost = TestPriceCost.Queryable.Where(c => c.PriceItem.Id == priceItem.Id).FirstOrDefault();
				priceCost.BaseCost = true;
				priceCost.Save();
				downloadLog = new PriceDownloadLog
				{
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
				rootPrice = TestSupplier.CreateTestSupplierWithPrice();
				scope.VoteCommit();
			}

			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				childPrice = TestSupplier.CreateTestSupplierWithPrice();

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
