using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class FarmaimpeksOKPFormalizer : BaseFormalizer, IPriceFormalizer
	{
		public FarmaimpeksOKPFormalizer(string filename, MySqlConnection connection, PriceFormalizationInfo data) : base(filename, connection, data)
		{
		}

		public void Formalize()
		{
			using (var reader = new FarmaimpeksOKPReader(_fileName)) {
				//FormalizePrice(reader);
				_priceInfo.IsUpdating = true;
				var parser = new BasePriceParser2(reader, _priceInfo, true);
				parser.Downloaded = Downloaded;
				parser.Formalize();
				formCount += parser.Stat.formCount;
				forbCount += parser.Stat.forbCount;
				unformCount += parser.Stat.unformCount;
				zeroCount += parser.Stat.zeroCount;
			}
		}

		public IList<string> GetAllNames()
		{
			return new List<string>();
		}
	}
}
