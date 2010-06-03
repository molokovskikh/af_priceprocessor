using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser;

namespace Inforoom.PriceProcessor.Waybills
{
	public class WaybillFormatDetector
	{
		public IDocumentParser DetectParser(string file, DocumentReceiveLog documentLog)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;
			if (extention == ".dbf")
				type = DetectDbfParser(file);
			else if (extention == ".sst")
				type = typeof (UkonParser);
			else if (extention == ".xls")
				type = typeof (Protek9Parser);
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
				(documentLog.Supplier.Id == 338 || documentLog.Supplier.Id == 4001
				|| documentLog.Supplier.Id == 7146 || documentLog.Supplier.Id == 5802))
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

		public Document DetectAndParse(DocumentReceiveLog log, string file)
		{
			var parser = DetectParser(file, log);
			var document = new Document(log);
			document.Parser = parser.GetType().Name;
			return parser.Parse(file, document);
		}
	}
}