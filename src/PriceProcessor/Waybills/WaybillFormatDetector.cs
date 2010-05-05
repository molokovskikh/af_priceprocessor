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
			else if ((extention == ".xml") || (extention == ".data"))
			{
				if (new SiaXmlParser().IsInCorrectFormat(file))
					type = typeof (SiaXmlParser);
				else if (new ProtekXmlParser().IsInCorrectFormat(file))
					type = typeof (ProtekXmlParser);
			}
			else if (extention == ".pd")
				type = typeof (ProtekParser);
			else if (extention == ".txt")
			{
				if (KatrenOrelTxtParser.CheckFileFormat(file))
					type = typeof (KatrenOrelTxtParser);
			}

			// ���� ��������� - ��� ����������� �����, ��� ���� ��������� ������ 
			// (������-�� ������ ��� �� ��� � � SiaParser, �� � ������� PRICE ���� ��� ���)
			if ((documentLog != null) && Moron_338_SpecialParser.CheckFileFormat(file) &&
				(documentLog.Supplier.Id == 338 || documentLog.Supplier.Id == 4001 || documentLog.Supplier.Id == 7146))
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
			if (Protek28Parser.CheckFileFormat(file))
				return typeof (Protek28Parser);
			if (IzhevskFarmParser.CheckFileFormat(file))
				return typeof (IzhevskFarmParser);
			if (SiaPermParser.CheckFileFormat(file))
				return typeof (SiaPermParser);
			if (AptekaHoldingSingleParser.CheckFileFormat(file))
				return typeof (AptekaHoldingSingleParser);
			if (AptekaHoldingSingleParser2.CheckFileFormat(file))
				return typeof(AptekaHoldingSingleParser2);
			if (AptekaHoldingIzhevskParser.CheckFileFormat(file))
				return typeof(AptekaHoldingIzhevskParser);
			if (RostaPermParser.CheckFileFormat(file))
				return typeof(RostaPermParser);
			if (FarmaimpeksIzhevskParser.CheckFileFormat(file))
				return typeof (FarmaimpeksIzhevskParser);
			if (KatrenOrelDbfParser.CheckFileFormat(file))
				return typeof (KatrenOrelDbfParser);
			if (GodunovDbfParser.CheckFileFormat(file))
				return typeof (GodunovDbfParser);
			throw new Exception("�� ������� ���������� ��� ������� ��� DBF �������");
		}
	}
}