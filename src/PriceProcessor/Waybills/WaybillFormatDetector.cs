using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Tools;
using ExcelLibrary.SpreadSheet;
using Inforoom.PriceProcessor.Waybills.Parser;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		public IDocumentParser DetectParser(string file, DocumentReceiveLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;

			// ���� ��� ��������� � ������� DBF �� ������-������������,
			// ������������ �� ����������� ��������, �� ��������� �� ������������ �������� ������ ��������
			if ((documentLog != null) && documentLog.Supplier.Id == 6256 && extention == ".dbf")
				type = typeof(Avesta_6256_SpecialParser);
			else
			{
				if (extention == ".dbf")
					type = DetectDbfParser(file);
				else if (extention == ".sst")
					type = typeof (UkonParser);
				else if (extention == ".xls")
					type = DetectXlsParser(file);
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
					type = DetectTxtParser(file);

				// ���� ��������� - ��� ����������� �����, ��� ���� ��������� ������ 
				// (������-�� ������ ��� �� ��� � � SiaParser, �� � ������� PRICE ���� ��� ���)
				if ((documentLog != null) && Moron_338_SpecialParser.CheckFileFormat(file) &&
				    (documentLog.Supplier.Id == 338 || documentLog.Supplier.Id == 4001
				     || documentLog.Supplier.Id == 7146 || documentLog.Supplier.Id == 5802
					 || documentLog.Supplier.Id == 21))
					type = typeof (Moron_338_SpecialParser);
			}
			if (type == null)
			{
				log4net.LogManager.GetLogger(typeof(WaybillService)).WarnFormat("�� ������� ���������� ��� ������� ���������. ���� {0}", file);
#if !DEBUG
				return null;
#else
				throw new Exception("�� ������� ���������� ��� �������");
#endif
			}

			var constructor = type.GetConstructors().Where(c => c.GetParameters().Count() == 0).FirstOrDefault();
			if (constructor == null)
				throw new Exception("� ���� {0} ��� ������������ ��� ����������");
			return (IDocumentParser)constructor.Invoke(new object[0]);
		}

		private static Type DetectDbfParser(string file)
		{
			var types = typeof (WaybillFormatDetector)
				.Assembly
				.GetTypes()
				.Where(t => t.Namespace.EndsWith("Waybills.Parser.DbfParsers") && t.IsPublic)
				.ToList();

			foreach (var type in types)
			{
				var detectFormat = type.GetMethod("CheckFileFormat", BindingFlags.Static | BindingFlags.Public);
				if (detectFormat == null)
					throw new Exception(String.Format("� ���� {0} ��� ������ ��� �������� �������, �������� ����� CheckFileFormat", type));
				var data = Dbf.Load(file);
				var result = (bool)detectFormat.Invoke(null, new object[] {data});
				if (result)
					return type;
			}
			return null;
		}

		private static Type DetectXlsParser(string file)
		{
			if (BssSpbXlsParser.CheckFileFormat(file))
				return typeof (BssSpbXlsParser);
			if (Protek9Parser.CheckFileFormat(file))
				return typeof(Protek9Parser);
			if (OACXlsParser.CheckFileFormat(file))
				return typeof (OACXlsParser);
			return null;
		}

		private static Type DetectTxtParser(string file)
		{
			if (KatrenOrelTxtParser.CheckFileFormat(file))
				return typeof (KatrenOrelTxtParser);
			if (RostaOmskParser.CheckFileFormat(file))
				return typeof (RostaOmskParser);
			if (KatrenOmskParser.CheckFileFormat(file))
				return typeof (KatrenOmskParser);
			if (DetstvoOmskParser.CheckFileFormat(file))
				return typeof (DetstvoOmskParser);
			if (RostaMskParser.CheckFileFormat(file))
				return typeof (RostaMskParser);
			if (TredifarmParser.CheckFileFormat(file))
				return typeof (TredifarmParser);
			return null;
		}

		public Document DetectAndParse(DocumentReceiveLog log, string file)
		{
			var parser = DetectParser(file, log);
			if (parser == null)
				return null;
			var document = new Document(log);
			document.Parser = parser.GetType().Name;
			return parser.Parse(file, document);
		}
	}
}