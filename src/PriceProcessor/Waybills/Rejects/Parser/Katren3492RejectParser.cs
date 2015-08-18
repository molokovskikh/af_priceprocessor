using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.POIFS.FileSystem;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 3492. На 15.07.2015 Типы отказов: dbf(2031), txt(560)
	/// Для формата txt парсера нет,так как в файле не указаны отказы
	/// Для dbf парсер не написан, так как в файле не указаны наименования товаров
	/// в следствии чего строки попадают в список плохих строк
	/// </summary>
	public class Katren3492RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
          if (filename.Contains(".txt"))
                ParseTXT(reject, filename);
            else
            {
                Logger.WarnFormat("Файл '{0}' не может быть распарсен, так как парсер {1} не умеет парсить данный формат файла", filename, GetType().Name);
            }
        }

        /// <summary>
        /// Парсер для формата файла TXT
        /// </summary>
        protected void ParseTXT(RejectHeader reject, string filename)
        {
            using (var reader = new StreamReader(File.OpenRead(filename), Encoding.GetEncoding(1251)))
            {
                string line;
                var rejectFound = false;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("Следующие расхождения с заявкой:"))
                    {
                        rejectFound = true;
                        continue;
                    }

                    if (line == "")
                        rejectFound = false;

                    if (!rejectFound)
                        continue;

                    var rejectLine = new RejectLine();
                    var fields = line.Split(new[] { "; - запрошено" }, StringSplitOptions.None);
                    var product = fields[0].Replace("Отказ по количеству:", "");
                    var fields2 = fields[1].Split(',');
                    var fields3 = fields2[0].Replace("шт.", "").Trim();
                    var ordered = fields3.Split('.');
                    var fields4 = fields2[1].Replace("к получению", "").Replace("шт.", "");
                    var received = fields4.Split('.');
                    reject.Lines.Add(rejectLine);
                    rejectLine.Product = product.Trim();
                    rejectLine.Ordered = NullableConvert.ToUInt32(ordered[0]);
                    var rejected = NullableConvert.ToUInt32(ordered[0]) - NullableConvert.ToUInt32(received[0]);
                    rejectLine.Rejected = rejected != null ? rejected.Value : 0;
                }
            }
        }
    }
}
