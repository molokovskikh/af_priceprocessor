using System.Data;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class PriceFormalizationInfoFixture
	{
		[Test]
		public void Update_price_if_update_flag_set()
		{
			var table = new DataTable();
			table.Columns.Add("region");
			table.Columns.Add("CostName");
			table.Columns.Add("SelfPriceName");
			table.Columns.Add("FirmShortName");
			table.Columns.Add("FirmCode");
			table.Columns.Add("CostCode");
			table.Columns.Add("PriceCode");
			table.Columns.Add("FormByCode");
			table.Columns.Add("CostType");
			table.Columns.Add("PriceType");
			table.Columns.Add("PriceItemId");
			table.Columns.Add("ParentSynonym");
			table.Columns.Add("RowCount");
			var row = table.Rows.Add("1", "", "", "", "1", "1", "1", false, 0, 1, 2, 1, 0);
			var info = new PriceFormalizationInfo(row, new Price { IsUpdate = true });
			Assert.That(info.IsUpdating, Is.True);
		}
	}
}