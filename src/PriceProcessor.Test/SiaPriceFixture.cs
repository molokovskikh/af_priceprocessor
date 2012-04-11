using System;
using System.Data;
using System.IO;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("Чинить")]
	public class SiaPriceFixture
	{
		[Test]
		public void Request_retio_should_be_readed_with_ignore_to_fraction_part()
		{
			TestHelper.Execute(@"update usersettings.PriceItems set RowCount = 5 where id = 781");
			TestHelper.Formalize<NativeDbfPriceParser>(Path.GetFullPath(@".\Data\781.dbf"));
			var data = TestHelper.Fill(@"
select * from farm.core0 c
where pricecode = 4649;");

			Assert.That(data.Tables[0].Rows.Count, Is.EqualTo(6), "Изменилось кол-во формализованных позиций для прайс-листа, значит порядок позиций в тесте будет неожидаемым");

			var coreTable = data.Tables[0];

			CheckCoreRowByRequestRatio(coreTable.Rows[0], "1022", null);
			CheckCoreRowByRequestRatio(coreTable.Rows[1], "1038", 50);
			CheckCoreRowByRequestRatio(coreTable.Rows[2], "10475", null);
			CheckCoreRowByRequestRatio(coreTable.Rows[3], "1083", 40);
			CheckCoreRowByRequestRatio(coreTable.Rows[4], "10860", 6);
			CheckCoreRowByRequestRatio(coreTable.Rows[5], "10861", 6);
		}

		private void CheckCoreRowByRequestRatio(DataRow dataRow, string code, int? requestRatio)
		{
			Assert.That(dataRow["Code"], Is.EqualTo(code), "Неожидаемая позиция в прайс-листе по полю Code");
			Assert.That(
				dataRow["RequestRatio"], 
				requestRatio.HasValue ? Is.EqualTo(requestRatio) : Is.EqualTo(DBNull.Value), 
				"У позиции некорректно формализовано поле RequestRatio");
		}
	}
}
