using System;
using System.Data;
using System.Linq;
using System.Text;
using Common.MySql;
using Dapper;
using Inforoom.Downloader;
using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Helpers;
using MimeKit;
using MySql.Data.MySqlClient;
using NHibernate;

namespace Inforoom.PriceProcessor.Downloader.MailHandler
{
	public partial class MailKitClient
	{
		private class SupplierSelector
		{
			public uint FirmCode { get; set; }
			public string EMailFrom { get; set; }
		}

		public void ProcessMessage(ISession session, MimeMessage message)
		{
			var emails = message.From.OfType<MailboxAddress>().Select(a => a.Address).ToArray();
			if (emails.Length == 0) {
				SendPublicErrorMessage($"У сообщения не указано ни одного отправителя.", message);
				_log.WarnFormat($"У сообщения не указано ни одного отправителя {message}");
				return;
			}

			var dtSources = session.Connection.Query<SupplierSelector>(@"			SELECT
																													s.Id FirmCode,
																													st.EMailFrom
																												FROM
																													Documents.Waybill_Sources st
																													INNER JOIN Customers.suppliers s ON s.Id = st.FirmCode
																													INNER JOIN farm.regions r ON r.RegionCode = s.HomeRegion
																												WHERE
																													st.SourceID = 1").ToList();

			if (dtSources.Count == 0) {
				_log.Info($"{nameof(MailKitClient)}: При загрузке источников получили пустую таблицу");
			}


			foreach (var emailAuthor in emails) {
				uint supplierId = 0;
				var sources = dtSources.Where(s => s.EMailFrom.ToLower() == emailAuthor.ToLower()).ToList();

				SupplierSelector source;

				if (sources.Count > 1) {

					// Нет адреса, клиента или другой информации об адресе доставки на этом этапе	//	SelectWaybillSourceForClient(sources, _addressId);

					SendPublicErrorMessage(String.Format("На адрес \"{0}\"" +
								"назначено несколько поставщиков. Определить какой из них работает с клиентом не удалось", emailAuthor), message);
					_log.Info(
						String.Format(
							$"{nameof(MailKitClient)}: На адрес \"{0}\"" +
								"назначено несколько поставщиков. Определить какой из них работает с клиентом не удалось", emailAuthor));
					throw new Exception(
						String.Format(
							"На адрес \"{0}\" назначено несколько поставщиков. Определить какой из них работает с клиентом не удалось",
							emailAuthor));
				} else if (sources.Count == 0)
				{
					var addition = String.Format("Количество записей в источниках - {0}", dtSources.Count);
					_log.Info(
						String.Format($"{nameof(MailKitClient)}: Не найдено записи в источниках, соответствующей адресу {0}. {1}",
							emailAuthor, addition));

					SendPublicErrorMessage(
						String.Format("Не найдено записи в источниках, соответствующей адресу {0}. {1}",
							emailAuthor, addition), message);

					continue;
				} else
					source = sources.First();

				supplierId = Convert.ToUInt32(source.FirmCode);

				//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
				bool matched = ProcessAttachments(session, message, supplierId, emailAuthor);

				sources.ForEach(s => { dtSources.Remove(s); });

				if (!matched) {
					SendPublicErrorMessage($"Письмо не распознано.", message);
					throw new Exception($"Письмо не распознано.");
				}
			}
		}
	}
}