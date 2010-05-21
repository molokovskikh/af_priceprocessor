using System.Data;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class UkonDbfParser : SiaParser
	{
		public UkonDbfParser()
		{
			Encoding = Encoding.GetEncoding(1251);
		}

		public new static bool CheckFileFormat(DataTable data)
		{
			return SiaParser.CheckFileFormat(data);
		}
	}
}