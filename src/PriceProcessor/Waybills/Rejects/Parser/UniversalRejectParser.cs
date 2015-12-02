using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Common.Tools;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Models;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 15445. На 07.07.2015 Типы отказов: dbf(4055).
	/// </summary>
	public class UniversalRejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".xml"))
				ParseXML(reject, filename);
			else {
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла",
					filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла XML
		/// </summary>
		protected void ParseXML(RejectHeader reject, string filename)
		{
			//получаем докуме5нт
			XDocument xdoc = XDocument.Load(filename);
			XElement doc = xdoc.Element("Reject");
			if (!doc.HasElements) {
				var err = string.Format("Не удалось получить файл с отказами '{0}' для лога документа {1}", filename, reject.Log.Id);
				Logger.Warn(err);
				return;
			}
			//парсим
			//получаем сначала динамику для проверки условий
			var xLines = doc.Elements("Line").Select(line => new
			{
				Code = line.Element("Code")?.Value,
				CodeCr = line.Element("CodeCr")?.Value,
				Product = line.Element("Product")?.Value,
				Producer = line.Element("Producer")?.Value,
				Rejected = NullableConvert.ToUInt32(line.Element("Rejected")?.Value),
				Ordered = NullableConvert.ToUInt32(line.Element("Ordered")?.Value),
				OrderId = NullableConvert.ToUInt32(line.Element("OrderId")?.Value),
			}).ToList();
			//проверяем условия
			xLines = xLines.Where(s => s.Code != null && s.Product != null && s.Rejected != null && s.OrderId != null).ToList();
			//подходящие эл.ты кидаем в RejectLine
			var xRejectLines = xLines.Select(line => new RejectLine()
			{
				Code = line.Code,
				CodeCr = line.CodeCr,
				Product = line.Product,
				Producer = line.Producer,
				Rejected = line.Rejected.Value,
				Ordered = line.Ordered.Value,
				OrderId = line.OrderId.Value,
			}).ToList();
			//RejectLine в список и отправляем дальше.
			reject.Lines.AddEach(xRejectLines);
		}
	}
}