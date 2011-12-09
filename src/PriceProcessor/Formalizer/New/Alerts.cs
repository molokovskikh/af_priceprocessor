using System;
using System.Text;
using Inforoom.Formalizer;
using log4net;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class Alerts
	{
		private static ILog _logger = LogManager.GetLogger(typeof (Alerts));

		public static void SendAlertToUserFail(StringBuilder stringBuilder, string subject, string body, PriceFormalizationInfo info)
		{
			if (stringBuilder.Length == 0)
				return;

			string fullPriceName;
			if (info.CostType == CostTypes.MultiColumn)
			{
				fullPriceName = String.Format("[Колонка] {0}", info.CostName);
			}
			else
			{
				fullPriceName = info.PriceName;
			}
			var fullSupplierName = String.Format("{0} - {1}", info.FirmShortName, info.Region);

			subject = String.Format(subject, fullPriceName, fullSupplierName);
			body = String.Format(
				body,
				fullPriceName,
				fullSupplierName,
				stringBuilder);

			_logger.DebugFormat("Сформировали предупреждение о настройках формализации прайс-листа: {0}", body);
			Mailer.SendUserFail(subject, body);
		}

		public static void ZeroCostAlert(StringBuilder stringBuilder, PriceFormalizationInfo info)
		{
			SendAlertToUserFail(
				stringBuilder,
				"PriceProcessor: В прайс-листе {0} поставщика {1} имеются ценовые колонки, полностью заполненные ценой \"0\"",
				@"
Здравствуйте!
  В прайс-листе {0} поставщика {1} имеются ценовые колонки, полностью заполненные ценой '0'.
  Список ценовых колонок:
{2}

С уважением,
  PriceProcessor.", info);
		}

		public static void ToManyZeroCostAlert(StringBuilder stringBuilder, PriceFormalizationInfo info)
		{
			SendAlertToUserFail(
				stringBuilder,
				"PriceProcessor: В прайс-листе {0} поставщика {1} имеются позиции с незаполненными ценами",
					@"
Здравствуйте!
  В прайс-листе {0} поставщика {1} имеются позиции с незаполненными ценами.
  Список ценовых колонок:
{2}

С уважением,
  PriceProcessor.", info);
		}

		public static void NotConfiguredAllert(StringBuilder sb, PriceFormalizationInfo info)
		{
			SendAlertToUserFail(
				sb,
				"PriceProcessor: В прайс-листе {0} поставщика {1} отсутствуют настроенные поля",
				@"
Здравствуйте!
В прайс-листе {0} поставщика {1} отсутствуют настроенные поля.
Следующие поля отсутствуют:
{2}

С уважением,
PriceProcessor.", info);
		}
	}
}