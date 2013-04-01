using System;
using System.Data;
using System.Reflection;
using Inforoom.PriceProcessor.Formalizer.Core;
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
			var info = FakeInfo();
			Assert.That(info.IsUpdating, Is.True);
		}

		public static PriceFormalizationInfo FakeInfo()
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

			var field = typeof(FormRules).GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (var fieldInfo in field) {
				var value = (String)fieldInfo.GetValue(null);
				if (!table.Columns.Contains(value))
					table.Columns.Add(value);
			}

			foreach (PriceFields pf in Enum.GetValues(typeof(PriceFields))) {
				var name = (PriceFields.OriginalName == pf) ? "FName1" : "F" + pf;
				if (!table.Columns.Contains(name))
					table.Columns.Add(name);
			}

			var row = table.Rows.Add("1", "", "", "", "1", "1", "1", false, 0, 1, 2, 1, 0);
			var info = new PriceFormalizationInfo(row, new Price { IsUpdate = true });
			return info;
		}
	}
}