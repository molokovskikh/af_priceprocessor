using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class UniversalFormalizer : BaseFormalizer, IPriceFormalizer
	{
		public UniversalFormalizer(string filename, MySqlConnection connection, PriceFormalizationInfo data)
			: base(filename, connection, data)
		{
		}

		public void Formalize()
		{
			using (var stream = File.OpenRead(_fileName)) {
				//В качестве решения по "Ошибка #9597 Трэдифарм Белгород" все прайс-листы с форматом UniversalFormalizer делаем "обновляемыми", 
				//т.к. BasePriceParser2 не умеет удалять "старые" позиции при простой формализации
				_priceInfo.IsUpdating = true;

				var reader = new UniversalReader(stream);

				var settings = reader.Settings().ToList();

				FormalizePrice(reader);
				With.Connection(c => {
					var command = new MySqlCommand("", c);
					UpdateIntersection(command, settings, reader.CostDescriptions);
				});
			}
		}

		public IList<string> GetAllNames()
		{
			using (var stream = File.OpenRead(_fileName)) {
				var reader = new UniversalReader(stream);

				//нужно считать настройки, если этого не сделать то данные прайса могут быть не прочитаны
				var settings = reader.Settings().ToList();
				return reader.Read()
					.Select(p => p.PositionName)
					.Where(n => !String.IsNullOrEmpty(n))
					.ToList();
			}
		}
	}
}