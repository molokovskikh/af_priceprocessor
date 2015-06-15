using System;
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
	public class VremyaFTK1422RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			var data = Dbf.Load(filename);
			for (var i = 1; i <= data.Rows.Count; i++) {
				var rejectLine = new RejectLine();
				reject.Lines.Add(rejectLine);
				rejectLine.Product = data.Rows[0][10].ToString();
				rejectLine.Code = data.Rows[0][9].ToString();
				rejectLine.Cost = NullableConvert.ToDecimal(data.Rows[0][13].ToString());
				rejectLine.Ordered = NullableConvert.ToUInt32(data.Rows[0][14].ToString());
				var rejected = NullableConvert.ToUInt32(data.Rows[0][15].ToString());
				rejectLine.Rejected = rejected != null ? rejected.Value : 0;
			}
		}
	}
}
