using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Castle.Components.DictionaryAdapter.Xml;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 7497. На 22.07.2015 Типы отказов: dbf(147), txt(1750) и xls(31)
	/// Для dbf и xls парсеров нет, так как файлы с отказами невозможно разобрать(определить отказы, заказы) 
	/// </summary>
	public class GodovalovPerm7497RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
			if (filename.Contains(".txt"))
				ParseTXT(reject, filename);
			else {
				Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
			}
		}

		/// <summary>
		/// Парсер для формата файла TXT
		/// </summary>
		protected void ParseTXT(RejectHeader reject, string filename)
		{
			using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251))) {
				string file = reader.ReadToEnd();
				if (file.Contains("HEAD")) {
					var parts = file.Split(new[] { "\r\n" }, StringSplitOptions.None);
					var rejectFound = false;
					for (var i = 0; i < parts.Count(); i++) {
						var fields = parts[i].Split(';');
						if (fields[0].Trim() == "Код товара") {
							rejectFound = true;
							continue;
						}

						if (parts[i] == "")
							rejectFound = false;

						if (!rejectFound)
							continue;

						var rejectLine = new RejectLine();
							fields = parts[i].Trim().Split(';');
							reject.Lines.Add(rejectLine);
							rejectLine.Code = fields[0].Trim();
							rejectLine.Product = fields[1].Trim();
							rejectLine.Ordered = NullableConvert.ToUInt32(fields[3].Trim());
							var rejected = NullableConvert.ToUInt32(fields[4].Trim());
							rejectLine.Rejected = rejected != null ? rejected.Value : 0;			
					}
				}
				else if (file.Contains("Ваша заявка")) {
					var parts = file.Split(new[] { "\r\n" }, StringSplitOptions.None);
					var rejectFound = false;

					for (var i = 0; i < parts.Count(); i++) {
						if (parts[i].Contains("L")) {
							rejectFound = true;
							continue;
						}

						if (parts[i] == "")
							rejectFound = false;
						if (!rejectFound)
							continue;
						if (parts.Length == 0)
							continue;

						var rejectLine = new RejectLine();
						reject.Lines.Add(rejectLine);
						var parts2 = parts[i].Trim().Split('|');
						rejectLine.Product = parts2[1].Trim();
						rejectLine.Ordered = NullableConvert.ToUInt32(parts2[2].Trim());
						var rejected = NullableConvert.ToUInt32(parts2[4].Trim());
						rejectLine.Rejected = rejected != null ? rejected.Value : 0;
					  }
				   }
				}
			}
		}
	}