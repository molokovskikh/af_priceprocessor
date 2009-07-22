using System;
using System.IO;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class SiaPriceFixture
	{
		[Test]
		public void Request_retio_should_be_readed_with_ignore_to_fraction_part()
		{
			TestHelper.Execute(@"update usersettings.PriceItems set RowCount = 5 where id = 781");
			TestHelper.Formilize<NativeDbfPriceParser>(Path.GetFullPath(@".\Data\781.dbf"), 781);
			var data = TestHelper.Fill(@"
select * from farm.core0 c
where pricecode = 4649;");
			Assert.That(data.Tables[0].Rows[0]["RequestRatio"], Is.EqualTo(50));
			Assert.That(data.Tables[0].Rows[1]["RequestRatio"], Is.EqualTo(DBNull.Value));
			Assert.That(data.Tables[0].Rows[2]["RequestRatio"], Is.EqualTo(50));
			Assert.That(data.Tables[0].Rows[3]["RequestRatio"], Is.EqualTo(DBNull.Value));
			Assert.That(data.Tables[0].Rows[4]["RequestRatio"], Is.EqualTo(40));
			Assert.That(data.Tables[0].Rows[5]["RequestRatio"], Is.EqualTo(6));
			Assert.That(data.Tables[0].Rows[6]["RequestRatio"], Is.EqualTo(6));
		}
	}
}
