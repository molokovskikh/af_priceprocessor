using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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
	/// Парсер для поставщика 1422. На 15.06.2015 Типы отказов: dbf(3158).
	/// </summary>
	public class VremyaFTK1422RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".dbf"))
				ParseDBF(reject, filename);
			else
			{
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла DBF
		/// </summary>
		protected void ParseDBF(RejectHeader reject, string filename)
		{
			DataTable data;
			try
			{
				data = Dbf.Load(filename);
			}
			catch (Exception e)
			{
				var err = string.Format("Не удалось получить файл с отказами '{0}' для лога документа {1}", filename, reject.Log.Id);
				Logger.Warn(err, e);
				return;
			}
			for (var i = 0; i < data.Rows.Count; i++)
			{
				var rejectLine = new RejectLine();
				reject.Lines.Add(rejectLine);
				rejectLine.Product = data.Rows[i][10].ToString();
				rejectLine.Code = data.Rows[i][9].ToString();
				rejectLine.Cost = NullableConvert.ToDecimal(data.Rows[i][13].ToString());
				rejectLine.Ordered = NullableConvert.ToUInt32(data.Rows[i][14].ToString());
				var rejected = NullableConvert.ToUInt32(data.Rows[i][15].ToString());
				rejectLine.Rejected = rejected != null ? rejected.Value : 0;
			}
		}
	}
}
