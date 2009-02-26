using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Formalizer;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class ParseDbfFileFixture
	{
		[Test]
		public void Pase_order_with_native_dbf_parser_should_be_same_as_oledb()
		{
			using (var connection = new MySqlConnection("server=testsql.analit.net;username=system; password=newpass; database=farm; pooling=true; Convert Zero Datetime=true;"))
			{
				connection.Open();
				var adapter = new MySqlDataAdapter(String.Format(@"
select
  pi.Id as PriceItemId,
  pi.RowCount,
  pd.PriceCode,
  PD.PriceName as SelfPriceName,
  PD.PriceType,
  pd.CostType,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  CD.FirmCode,
  CD.ShortName as FirmShortName,
  CD.FirmStatus,
  FR.JunkPos as SelfJunkPos,
  FR.AwaitPos as SelfAwaitPos,
  FR.VitallyImportantMask as SelfVitallyImportantMask,
  ifnull(pd.ParentSynonym, pd.PriceCode) as ParentSynonym,
  PFR.*,
  pricefmts.FileExtention,
  pricefmts.ParserClassName
from
  usersettings.PriceItems pi,
  usersettings.pricescosts pc,
  UserSettings.PricesData pd,
  UserSettings.ClientsData cd,
  Farm.formrules FR,
  Farm.FormRules PFR,
  farm.pricefmts 
where
    pi.Id = {0}
and pc.PriceItemId = pi.Id
and pd.PriceCode = pc.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.FirmCode = pd.FirmCode
and FR.Id = pi.FormRuleId
and PFR.Id= if(FR.ParentFormRules, FR.ParentFormRules, FR.Id)
and pricefmts.ID = PFR.PriceFormatId", "552"), connection);
				var data = new DataSet();
				adapter.Fill(data);
				connection.Close();
				var parser = new NativeDbfPriceParser(Path.GetFullPath(@".\Data\552.dbf"), connection, data.Tables[0]);
				parser.Formalize();

				var result = new DataSet();
				adapter.SelectCommand.CommandText = @"
select *
from farm.core0 c
  join corecosts cc on c.id = cc.Core_Id
where pricecode = 3331";
				adapter.Fill(result);

				var etalon = new DataSet();
				etalon.ReadXml(@".\Data\552.xml", XmlReadMode.ReadSchema);
				Compare(result, etalon);
			}
		}

		private void Compare(DataSet result, DataSet etalon)
		{
			Assert.That(result.Tables.Count, Is.EqualTo(1), "нет таблицы с данными");
			Assert.That(etalon.Tables.Count, Is.EqualTo(1), "нет таблицы с данными");
			Assert.That(result.Tables[0].Rows.Count, Is.EqualTo(etalon.Tables[0].Rows.Count));
			Assert.That(result.Tables[0].Rows.Count, Is.GreaterThan(0));
			for (int i = 0; i < result.Tables[0].Rows.Count; i++)
			{
				var resultRow = result.Tables[0].Rows[i];
				var etalonRow = etalon.Tables[0].Rows[i];
				foreach (DataColumn column in result.Tables[0].Columns)
				{
					if (column.ColumnName == "Id" || column.ColumnName == "Core_Id")
						continue;
					Assert.That(resultRow[column.ColumnName], Is.EqualTo(etalonRow[column.ColumnName]), "не сошлось значение в колонке {0}", column.ColumnName);
				}
			}
		}

		[Test]
		public void BuildEtalon()
		{
			using (var connection = new MySqlConnection("server=sql.analit.net;username=Kvasov; password=ghjgtkkth; database=farm; pooling=true; Convert Zero Datetime=true;"))
			{
				connection.Open();
				var adapter = new MySqlDataAdapter(@"
select *
from farm.core0 c
  join corecosts cc on c.id = cc.Core_Id
where pricecode = 3331", connection);
				var data = new DataSet();
				adapter.Fill(data);
				using (var file = File.Create("552.xml"))
				{
					data.WriteXml(file, XmlWriteMode.WriteSchema);					
				}
			}
		}
	}
}
