using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class FarmaimpeksOKPFormalizer : BaseFormalizer, IPriceFormalizer
	{
		public FarmaimpeksOKPFormalizer(string filename, PriceFormalizationInfo data) : base(filename, data)
		{
		}

		public void Formalize()
		{
			using (var reader = new FarmaimpeksOKPReader(_fileName)) {
				Info.IsUpdating = true;
				var parser = new BasePriceParser(reader, Info, true);
				parser.Downloaded = Downloaded;
				parser.Formalize();
				Stat = parser.Stat;
			}
		}

		public IList<string> GetAllNames()
		{
			return new List<string>();
		}
	}
}
