using System;
using System.IO;
using System.ServiceModel;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using log4net;
using LumiSoft.Net.Mime;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("Тест что бы разбирать проблемные ситуации")]
	public class Troubleshoot
	{
		[Test, Ignore]
		public void shoot_it()
		{
			//7399851
			Console.WriteLine(ExtractFileFromAttachment(@"C:\7399851.eml", "СводныйПрайсЧ.rar", "СводныйПрайсЧ.txt"));
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

	}
}
