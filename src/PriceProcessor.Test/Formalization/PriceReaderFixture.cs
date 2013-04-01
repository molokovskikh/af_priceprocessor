using System.Collections.Generic;
using System.Data;
using System.Linq;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using NUnit.Framework;
using PriceProcessor.Test.Models;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class PriceReaderFixture
	{
		[Test]
		public void Ignore_case()
		{
			var info = PriceFormalizationInfoFixture.FakeInfo();
			info.FormRulesData.Rows[0]["FName1"] = "F1";
			var reader = new PriceReader(new FakePrser(), "", info);
			reader.CostDescriptions = new List<CostDescription> {
				new CostDescription {
					FieldName = "f2"
				}
			};
			var positions = reader.Read().ToList();
			Assert.That(positions.Count, Is.EqualTo(1));
			var costs = positions[0].Core.Costs;
			Assert.That(costs.Length, Is.EqualTo(1));
			Assert.That(costs[0].Value, Is.EqualTo(50));
		}
	}

	public class FakePrser : IParser
	{
		public DataTable Parse(string filename)
		{
			var table = new DataTable();
			table.Columns.Add("F1");
			table.Columns.Add("F2");
			var row = table.NewRow();
			row["F1"] = "Папаверин";
			row["F2"] = "50";
			table.Rows.Add(row);
			return table;
		}

		public DataTable Parse(string filename, bool specialProcessing)
		{
			return Parse(filename);
		}
	}
}