using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Castle.Components.DictionaryAdapter.Xml;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.POIFS.FileSystem;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	public class AstiPlus12714RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			var data = Dbf.Load(filename);
				for (var i = 1; i <= data.Rows.Count; i++) {
					var rejectLine = new RejectLine();
					reject.Lines.Add(rejectLine);
					rejectLine.Product = data.Rows[0][1].ToString();
					rejectLine.Producer = data.Rows[0][2].ToString();
					rejectLine.Ordered = NullableConvert.ToUInt32(data.Rows[0][3].ToString());
					var rejected = NullableConvert.ToUInt32(data.Rows[0][4].ToString());
					rejectLine.Cost = NullableConvert.ToDecimal(data.Rows[0][5].ToString());
					rejectLine.Rejected = rejected != null ? rejected.Value : 0;
				}
		}
	}
}