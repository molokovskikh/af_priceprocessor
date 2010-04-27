using System;
using System.Collections.Generic;
using System.Text;
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
		public abstract DocType Type { get; }

		public abstract int TypeID
		{
			get;
		}

		public abstract string FolderName
		{
			get;
		}

		public abstract string Domen
		{
			get;
		}

		public bool ParseEmail(string email, out int ClientCode)
		{
			ClientCode = 0;
			int Index = email.IndexOf("@" + Domen);
			if (Index > -1)
			{
				if (int.TryParse(email.Substring(0, Index), out ClientCode))
				{
					return true;
				}
				else
					return false;
			}
			else
				return false;
		}
	}

	public class WaybillType : InboundDocumentType
	{
		public override DocType Type
		{
			get { return DocType.Waybill; }
		}

		public override int TypeID
		{
			get { return 1; }
		}

		public override string FolderName
		{
			get { return "Waybills"; }
		}

		public override string Domen
		{
			get { return "waybills.analit.net"; }
		}
	}

	public class RejectType : InboundDocumentType
	{
		public override DocType Type
		{
			get { return DocType.Reject; }
		}

		public override int TypeID
		{
			get { return 2; }
		}

		public override string FolderName
		{
			get { return "Rejects"; }
		}

		public override string Domen
		{
			get { return "refused.analit.net"; }
		}
	}

}
