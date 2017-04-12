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
		private PriceFormalizationInfo info;
		private DataTable table;
		private List<CostDescription> costDescriptions;

		[SetUp]
		public void Setup()
		{
			costDescriptions = new List<CostDescription>();
			table = new DataTable();
			table.Columns.Add("F1");
			table.Columns.Add("F2");
			var row = table.NewRow();
			row["F1"] = "Папаверин";
			row["F2"] = "50";
			table.Rows.Add(row);

			info = PriceFormalizationInfoFixture.FakeInfo();
			info.FormRulesData.Rows[0]["FName1"] = "F1";
		}

		[Test]
		public void Ignore_case()
		{
			costDescriptions = new List<CostDescription> {
				new CostDescription {
					FieldName = "f2"
				}
			};

			var positions = Read();
			Assert.That(positions.Count, Is.EqualTo(1));
			var costs = positions[0].Offer.Costs;
			Assert.That(costs.Length, Is.EqualTo(1));
			Assert.That(costs[0].Value, Is.EqualTo(50));
		}

		[Test]
		public void Read_producer_cost()
		{
			info.FormRulesData.Rows[0]["FProducerCost"] = "ProducerCost";
			table.Columns.Add("ProducerCost");
			table.Rows[0]["ProducerCost"] = 15;

			var positions = Read();
			Assert.That(positions[0].Offer.ProducerCost, Is.EqualTo(15));
		}

		private List<FormalizationPosition> Read()
		{
			var reader = new PriceReader(new FakePrser(table), "", info);
			reader.CostDescriptions = costDescriptions;
			var positions = reader.Read().ToList();
			return positions;
		}
	}

	public class FakePrser : IParser
	{
		private DataTable table;

		public FakePrser(DataTable table)
		{
			this.table = table;
		}

		public DataTable Parse(string filename)
		{
			return table;
		}

		public DataTable Parse(string filename, bool specialProcessing)
		{
			return Parse(filename);
		}
	}
}