using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;

namespace Inforoom.Downloader.Documents
{
	//Класс содержит название полей из таблицы Document.Waybill_Sources
	public sealed class WaybillSourcesTable
	{
		public static string colFirmCode = "FirmCode";
		public static string colShortName = "ShortName";
		public static string colEMailFrom = "EMailFrom";
		public static string colReaderClassName = "ReaderClassName";
	}

	//Абстрактный класс, описывающий тип документа
	public abstract class InboundDocumentType
	{
		public DocType DocType { get; protected set; }

		public string FolderName { get; protected set;  }

		public string Domen { get; protected set; }

		public bool ParseEmail(string email, out uint clientCode)
		{
			clientCode = 0;
			int Index = email.IndexOf("@" + Domen);
			if (Index > -1)
			{
				if (uint.TryParse(email.Substring(0, Index), out clientCode))
				{
					return true;
				}
				return false;
			}
			return false;
		}
	}

	public class WaybillType : InboundDocumentType
	{
		public WaybillType()
		{
			DocType = DocType.Waybill;
			FolderName = "Waybills";
			Domen = "waybills.analit.net";
		}
	}

	public class RejectType : InboundDocumentType
	{
		public RejectType()
		{
			DocType = DocType.Reject;
			FolderName = "Rejects";
			Domen = "refused.analit.net";
		}
	}

}
