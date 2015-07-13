using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using NPOI.POIFS.FileSystem;

namespace Inforoom.PriceProcessor.Waybills.Rejects.Parser
{
	/// <summary>
	/// Парсер для поставщика 1962. На 07.07.2015 Типы отказов: txt(318), sst(166) и dbf(3311).
	/// Для DBF парсера нет,так как файл не содержит ни наименования товаров, ни отказов
	/// Парсеров для поставщика 1962 нет,так как файлы с отказами невозможно разобрать(определить наименование, отказы, заказы) 
	/// </summary>
	public class Profitmed1962RejectParser : RejectParser
	{
		public override void Parse(RejectHeader reject, string filename)
		{
		}
	}
}
