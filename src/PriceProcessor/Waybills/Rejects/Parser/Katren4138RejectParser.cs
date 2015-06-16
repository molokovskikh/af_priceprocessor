using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 4138. На 15.06.2015 Типы отказов: xls(9546), txt(4976), dbf(210) и zip(1)
	/// </summary>
	public class Katren4138RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".xls"))
				ParseXLS(reject, filename);
			else {
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла XLS
		/// Но на самом деле это файлы dbf
		/// </summary>
		protected void ParseXLS(RejectHeader reject, string filename)
		{
			var data = Dbf.Load(filename);
			for (var i = 0; i < data.Rows.Count; i++)
			{
				var rejectLine = new RejectLine();
				reject.Lines.Add(rejectLine);
				rejectLine.Code = data.Rows[i][0].ToString();
				rejectLine.Product = data.Rows[i][1].ToString();
				rejectLine.Producer = data.Rows[i][2].ToString();

				//высчитываем сколько отказов (из того сколько заказано вычитаем то сколько доставлено)
				var ordered = NullableConvert.ToUInt32(data.Rows[i][3].ToString());
				var delivered = NullableConvert.ToUInt32(data.Rows[i][4].ToString());
				rejectLine.Ordered = ordered;

				//если в ячейке доставлено пусто-считаем что доставлено ноль
				if (delivered == null)
					delivered = 0;

				var rejected = ordered - delivered;
				rejectLine.Rejected = rejected != null ? rejected.Value : 0;
			}	
		}
	}
}
