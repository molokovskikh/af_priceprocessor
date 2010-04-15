using System;
using System.IO;
using System.Linq;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.Multifile;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		public IDocumentParser DetectParser(string file, DocumentLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;
			if (extention == ".dbf")
				type = DetectDbfParser(file);
			else if (extention == ".sst")
				type = typeof (UkonParser);
			else if (extention == ".xml")
			{
				if (new SiaXmlParser().IsInCorrectFormat(file))
					type = typeof (SiaXmlParser);
				else if (new ProtekXmlParser().IsInCorrectFormat(file))
					type = typeof (ProtekXmlParser);
			}
			else if (extention == ".pd")
				type = typeof (ProtekParser);

			// ���� ��������� - ��� ����������� �����, ��� ���� ��������� ������ 
			// (������-�� ������ ��� �� ��� � � SiaParser, �� � ������� PRICE ���� ��� ���)
			if ((documentLog != null) && (documentLog.Supplier.Id == 338))
				type = typeof (Moron_338_SpecialParser);

			if (type == null)
				throw new Exception("�� ������� ���������� ��� �������");

			var constructor = type.GetConstructors().Where(c => c.GetParameters().Count() == 0).FirstOrDefault();
			if (constructor == null)
				throw new Exception("� ���� {0} ��� ������������ ��� ����������");
			return (IDocumentParser)constructor.Invoke(new object[0]);
		}

		private static Type DetectDbfParser(string file)
		{
			if (MoronDbfParser.CheckFileFormat(file))
				return typeof (MoronDbfParser);
			if (UkonDbfParser.IsInCorrectFileFormat(file))
				return typeof (UkonDbfParser);
			if (SiaParser.CheckFileFormat(file))
				return typeof (SiaParser);
			if (GenezisDbfParser.CheckFileFormat(file))
				return typeof (GenezisDbfParser);
			if (AptekaHoldingParser.CheckFileFormat(file))
				return typeof (AptekaHoldingParser);
			throw new Exception("�� ������� ���������� ��� ������� ��� DBF �������");
		}
	}
}