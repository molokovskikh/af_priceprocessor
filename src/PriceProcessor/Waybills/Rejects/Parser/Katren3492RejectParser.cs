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
		}
	}
}
