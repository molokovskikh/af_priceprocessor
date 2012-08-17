using System;
using System.Data;
using System.Linq;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class ProducerSynonymFixture
	{
		[Test, Ignore("Починить")]
		public void Producer_synonym_should_be_created_if_20_percent_producers_is_well_known()
		{
			var file = @"..\..\Data\688-wrong-column-for-producers.txt";
			var priceItemId = 688;

			TestHelper.Execute(@"
delete from farm.SynonymFirmCr
where (Synonym like '26438' or Synonym like '35894' or Synonym like '30650' 
	or Synonym like '20164' or Synonym like '26436' or Synonym like '30649'
	or Synonym like '31550' or Synonym like '30648' or Synonym like '28136'
	or Synonym like '30171') and priceCode = 5
");
			var maxSynonymCode = Convert.ToUInt32(TestHelper.Fill(@"select max(SynonymFirmCrCode) from farm.SynonymFirmCr where PriceCode = 5").Tables[0].Rows[0][0]);

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-assortment-rules.xml", priceItemId));

			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);

			var synonyms = TestHelper
				.Fill(String.Format(@"select Synonym from farm.SynonymFirmCr where PriceCode = 5 and SynonymFirmCrCode > {0}", maxSynonymCode))
				.Tables[0]
				.Rows
				.Cast<DataRow>()
				.Select(r => r[0].ToString())
				.ToList();

			Assert.That(synonyms, Is.Empty);
		}
	}
}