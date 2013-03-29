using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using MySql.Data.MySqlClient;

namespace PriceProcessor.Test.Formalization
{
	public class FakeParser : BasePriceParser
	{
		public FakeParser(IReader reader, PriceFormalizationInfo priceInfo) : base(reader, priceInfo)
		{
		}

		public MySqlDataAdapter DaSynonymFirmCr
		{
			get { return daSynonymFirmCr; }
		}

		public DataTable DtSynonymFirmCr
		{
			get { return dtSynonymFirmCr; }
		}

		public MySqlConnection Connection
		{
			get { return _connection; }
		}
	}

	public class FakeReader : IReader
	{
		public IEnumerable<FormalizationPosition> Read()
		{
			return new List<FormalizationPosition>();
		}

		public List<CostDescription> CostDescriptions { get; set; }
		public IEnumerable<Customer> Settings()
		{
			return new List<Customer>();
		}

		public void SendWarning(PriceLoggingStat stat)
		{
		}
	}
}
