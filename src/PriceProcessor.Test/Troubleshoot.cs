using System;
using System.IO;
using System.ServiceModel;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using log4net;
using LumiSoft.Net.Mime;
using NUnit.Framework;
using RemotePriceProcessor;
using FileHelper = Inforoom.PriceProcessor.FileHelper;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("Тест что бы разбирать проблемные ситуации")]
	public class Troubleshoot
	{
		[Test, Ignore]
		public void shoot_it()
		{
			var file = @"C:\tovar_out.xml";
			var formalizer = PricesValidator.Validate(file, Path.GetTempFileName(), 2601);
			formalizer.Formalize();
			Assert.That(formalizer.formCount, Is.GreaterThan(0));
		}

		[Test]
		public void Get_sert()
		{
			var handler = new ProtekWaybillHandler();
			var uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			getSertImagesResponse response = null;
			handler.WithService(uri, s => {
				response = s.getSertImages(new getSertImagesRequest(123108, 1064974, 4960614));
			});
			foreach (var image in response.@return.sertImage)
			{
				Console.WriteLine(image.docId);
				Console.WriteLine(image.imageSize);
				Console.WriteLine(image.saveTypeId);
				Console.WriteLine(image.leafNumber);
				File.WriteAllBytes(image.docId.ToString() + ".tif", image.image);
			}
			response.ToXml(Console.Out);
		}

		private string ExtractFileFromAttachment(string filename, string archFileName, string externalFileName)
		{
			using (var fs = new FileStream(filename, FileMode.Open, 
				FileAccess.Read, FileShare.Read))
			{
				var logger = LogManager.GetLogger(GetType());
				try
				{
					var message = Mime.Parse(fs);
					message = UueHelper.ExtractFromUue(message, Path.GetTempPath());

					var attachments = message.GetValidAttachements();
					foreach (var entity in attachments)
					{
						var attachmentFilename = entity.GetFilename();

						if (!FileHelper.CheckMask(attachmentFilename, archFileName) &&
							!FileHelper.CheckMask(attachmentFilename, externalFileName))
							continue;
						entity.DataToFile(attachmentFilename);
						return filename;
					}
				}
				catch (Exception ex)
				{
					logger.ErrorFormat(
						"Возникла ошибка при попытке перепровести прайс. Не удалось обработать файл {0}. Файл должен быть письмом (*.eml)\n{1}",
						filename, ex);
					string errorMessage = String.Format("Не удалось перепровести прайс.");
					Mailer.SendFromServiceToService("Ошибка при перепосылке прайс-листа", String.Format("Имя файла: {0}\n{1}", filename, ex.ToString()));
					throw new FaultException<string>(errorMessage, new FaultReason(errorMessage));
				}
			}
			return null;
		}

		[Test, Ignore]
		public void TryGetAllNames()
		{
			string conn = Literals.ConnectionString();
			PriceProcessItem item = PriceProcessItem.GetProcessItem(1253);
			item.GetAllNames();

/*			uint priceItemId = 183;
			Stream stream = File.OpenRead(@"D:\Temp\Черная_Запрещенная_матрица_211011.xls");

			var parameter = new FilePriceInfo() {
				PriceItemId = priceItemId,
				Stream = stream,
				LogInformation = new LogInformation() {
					ComputerName = Environment.MachineName,
					UserName = Environment.UserName
				}
			};

			var service = new WCFPriceProcessorService();
			service.PutFileToBase(parameter);

			PriceProcessItem item = PriceProcessItem.GetProcessItem(priceItemId);
			item.GetAllNames();
			*/
		}
	}
}
