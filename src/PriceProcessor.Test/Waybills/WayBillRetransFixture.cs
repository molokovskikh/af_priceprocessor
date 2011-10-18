using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net.Config;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NUnit.Framework;
using Test.Support;
using Test.Support.Helpers;
using Common.Tools;
using System.Data;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Reflection;
using Common.MySql;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture, Ignore("Это не тест! Используется в крайнем случае для перепроведения накладных")]
	/*Если требуется, нужно изменить строку подключения к БД и путь DocumentPath*/
	public class WayBillRetransFixture
	{
		private DataTable dtLogs;
		private MySqlConnection connection;
		private MySqlCommand command;
		private MySqlDataAdapter dataAdapter;
		private DataColumn RowId;
		private DataColumn FirmCode;
		private DataColumn ClientCode;
		private DataColumn FileName;
		private DataColumn AddressId;
		private DataSet dtSet;

		[SetUp]
		public void Setup()
		{			
            string conn = Literals.ConnectionString();		
            connection = new MySqlConnection(Literals.ConnectionString());
			command = new MySqlCommand();
			command.Connection = connection;
			dataAdapter = new MySqlDataAdapter(command);
			dtLogs = new System.Data.DataTable();
			dtLogs.Columns.AddRange(new System.Data.DataColumn[]
			                          	{
											RowId = new System.Data.DataColumn(){AllowDBNull = false, ColumnName = "RowId", DataType = typeof(long)},
											FirmCode = new System.Data.DataColumn(){AllowDBNull = true, ColumnName = "FirmCode", DataType = typeof(int)},
											ClientCode = new System.Data.DataColumn(){AllowDBNull = true, ColumnName = "ClientCode", DataType = typeof(int)},
											FileName = new System.Data.DataColumn(){AllowDBNull = true, ColumnName = "FileName", DataType = typeof(string)},
											AddressId = new System.Data.DataColumn(){AllowDBNull = true, ColumnName = "AddressId", DataType = typeof(int)}
			                          	});
			dtLogs.PrimaryKey = new System.Data.DataColumn[] { RowId };
			dtLogs.TableName = "Logs";
			dtSet = new DataSet();
			dtSet.Tables.Add(dtLogs);
		}

		private void FillLogs()
		{
			try
			{
				connection.Open();
				command.CommandText =
@"SELECT dl.RowId, dl.FirmCode, dl.ClientCode, dl.FileName, dl.AddressId
FROM
(select distinct dl.FirmCode from `logs`.document_logs dl
inner join documents.DocumentHeaders dh on dl.RowId = dh.DownloadId
and dl.LogTime >= '2011-01-01' and dh.Parser is not null
) S
inner join `logs`.document_logs dl on dl.FirmCode = S.FirmCode
left join documents.DocumentHeaders dh on dl.RowId = dh.DownloadId
where dl.LogTime > '2011-05-04 21:00:00' and dl.LogTime < '2011-05-05 10:00:00' and dh.Id is null and dl.ClientCode is not null ;";
			}
			finally
			{
				dataAdapter.Fill(dtLogs);
				dtLogs.WriteXml(@"D:\dtLogs.xml");				
				connection.Close();
			}
		}

		[Test, Ignore]			
		public void ReparseWaybills()
		{
			FillLogs();
			foreach (DataRow log in dtLogs.Rows)
			{
				uint rowId = Convert.ToUInt32(log[RowId]);				
				DocumentReceiveLog doc_log = DocumentReceiveLog.TryFind(rowId);
				List<DocumentReceiveLog> logs = new List<DocumentReceiveLog>();
				logs.Add(doc_log);
				WaybillService.ParseWaybills(logs);

				IList<Document> doc_list = Document.Queryable.Where(d => d.Log.Id == doc_log.Id).Select(d => d).ToList();
				if(doc_list.Count() == 1)
				{
					With.Connection(c =>
					                	{
					                		MySqlHelper.ExecuteNonQuery(c,
					                		                            @"
update  `logs`.DocumentSendLogs set UpdateId = null, Committed = 0 where DocumentId = ?DocId",
					                		                            new MySqlParameter("?DocId", rowId));
					});
				}

			}
		}

		[Test, Ignore]		
		public void ReparseWaybill()
		{
			uint rowId = 8068636;				
				DocumentReceiveLog doc_log = DocumentReceiveLog.TryFind(rowId);
				List<DocumentReceiveLog> logs = new List<DocumentReceiveLog>();
				logs.Add(doc_log);
				WaybillService.ParseWaybills(logs);

				IList<Document> doc_list = Document.Queryable.Where(d => d.Log.Id == doc_log.Id).Select(d => d).ToList();
				if(doc_list.Count() == 1)
				{
					With.Connection(c =>
					                	{
					                		MySqlHelper.ExecuteNonQuery(c,
					                		                            @"
update  `logs`.DocumentSendLogs set UpdateId = null, Committed = 0 where DocumentId = ?DocId",
					                		                            new MySqlParameter("?DocId", rowId));
					});
				}

		}

		[Test, Ignore]
		public void ParseWorkWaybill()
		{
			var conn = Literals.ConnectionString();
			uint rowId = 9835721;
			DocumentReceiveLog doc_log = DocumentReceiveLog.TryFind(rowId);
			WaybillService.ParserDocument(doc_log);
		}

        [Test, Ignore("Не тест! Вспомогательный функционал")]
        public void CopyOrdersOldToLocal()
        {
            With.DefaultConnectionStringName = "Main";
            var ds = TestHelper.Fill("select * from ordersold.ordershead where AddressId is null or UserId is null group by ClientCode;");

            With.DefaultConnectionStringName = Literals.GetConnectionName();

            var table = ds.Tables[0];
            var rowCnt = table.Rows.Count;

            With.Connection(c =>
                                {
                                    var command = new MySqlCommand(@"
DROP TABLE IF EXISTS `OrdersOld`.`ordershead`;
CREATE TABLE  `OrdersOld`.`ordershead` (
  `RowID` int(10) unsigned NOT NULL DEFAULT '0',
  `WriteTime` datetime NOT NULL DEFAULT '0000-00-00 00:00:00',
  `ClientCode` int(10) unsigned NOT NULL DEFAULT '0',
  `UserId` int(10) unsigned DEFAULT NULL,
  `AddressId` int(10) unsigned DEFAULT NULL,
  `PriceCode` int(10) unsigned NOT NULL DEFAULT '0',
  `RegionCode` bigint(20) unsigned NOT NULL DEFAULT '0',
  `PriceDate` datetime DEFAULT NULL,
  `SubmitDate` datetime DEFAULT NULL,
  PRIMARY KEY (`WriteTime`,`RegionCode`,`PriceCode`,`RowID`) USING BTREE,
  UNIQUE KEY `RowId` (`RowID`),
  KEY `Clients` (`ClientCode`,`UserId`,`AddressId`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251 ROW_FORMAT=DYNAMIC;
", c);
                                    command.ExecuteNonQuery();
                                });               

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                With.Connection(c =>
                                    {
                                        var command =
                                            new MySqlCommand(
                                                @"
insert into ordersold.ordershead(RowId, WriteTime, ClientCode, UserId, AddressId, PriceCode, RegionCode, 
                                 PriceDate, SubmitDate) 
values(?RowId, ?WriteTime, ?ClientCode, ?UserId, ?AddressId, ?PriceCode, ?RegionCode, 
                                 ?PriceDate, ?SubmitDate)", c);
                                        command.Parameters.AddWithValue("?RowId", row["RowId"]);
                                        command.Parameters.AddWithValue("?WriteTime", row["WriteTime"]);
                                        command.Parameters.AddWithValue("?ClientCode", row["ClientCode"]);
                                        command.Parameters.AddWithValue("?UserId", row["UserId"]);
                                        command.Parameters.AddWithValue("?AddressId", row["AddressId"]);
                                        command.Parameters.AddWithValue("?PriceCode", row["PriceCode"]);
                                        command.Parameters.AddWithValue("?RegionCode", row["RegionCode"]);
                                        command.Parameters.AddWithValue("?PriceDate", row["PriceDate"]);
                                        command.Parameters.AddWithValue("?SubmitDate", row["SubmitDate"]);
                                        command.ExecuteNonQuery();
                                    });

            }
        }

        public struct WaybillOrders
        {
            public int wID { get; set; }
            public int oID { get; set; }
        }

        [Test, Ignore("Не тест, вспомогательный функционал. Проверяет корректность заполнения таблицы documents.waybillorders")]
        public void WaybillOrdersCheck()
        {
            With.DefaultConnectionStringName = "Local";            
            var ds = TestHelper.Fill(@"
SELECT wo.DocumentLineId, wo.OrderLineId FROM documents.waybillorders wo
join orders.orderslist ol on wo.OrderLineId = ol.RowId
join documents.documentbodies db on wo.DocumentLineId = db.Id
join documents.documentheaders dh on db.DocumentId = dh.Id
join logs.document_logs dl on dl.RowId = dh.DownloadId and dl.LogTime >= '2011-10-10 00:00' and dl.LogTime <= '2011-10-17 00:00';");

            List<WaybillOrders> woList = new List<WaybillOrders>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                WaybillOrders wo = new WaybillOrders();
                wo.wID = Convert.ToInt32(row["DocumentLineId"]);
                wo.oID = Convert.ToInt32(row["OrderLineId"]);
                woList.Add(wo);
            }
            
            using(new SessionScope())
            {
                int total = 0;
                int success = 0;

                while (woList.Count > 0)
                {
											
						var orderlineId = woList.First().oID;

						var oline = TestOrderItem.TryFind(Convert.ToUInt32(orderlineId));

						if (oline == null)
							break;

						var wotmp = woList.Where(wo => wo.oID == orderlineId).ToList();

						var wlineIds = wotmp.Select(wo => wo.wID).Distinct().ToList();

						var wlines = wlineIds.Select(l => TestWaybillLine.TryFind(Convert.ToUInt32(l))).ToList();
						wlines = wlines.Where(w => w != null).ToList();

						long orderQnt = 0;
						long waybillQnt = 0;

						if (wlines.Count > 0)
						{
							foreach (var wline in wlines)
							{
								wotmp = woList.Where(wo => wo.wID == wline.Id).ToList();

								var olineIds = wotmp.Select(wo => wo.oID).Distinct().ToList();
								var olines =
									olineIds.Select(l => TestOrderItem.TryFind(Convert.ToUInt32(l))).Where(w => w != null).
										ToList();
								orderQnt += olines.Sum(l => l.Quantity);
								wotmp.Each(wo => woList.Remove(wo));
							}
							waybillQnt = wlines.Sum(l => l.Quantity); // коичество в позиции накладной
						}
						if (orderQnt == waybillQnt) success++;
						total++;
					
                }
				double res = (double)success / total * 100f;
            }
        }

		[Test, Ignore("Не тест, вспомогательный функционал. Проверяет корректность заполнения таблицы documents.waybillorders")]
		public void WaybillOrdersCheck2()
		{
			With.DefaultConnectionStringName = "Local";
			var ds = TestHelper.Fill(@"
SELECT ol.ProductId orderProductId, db.ProductId waybillProductId FROM documents.waybillorders wo
join orders.orderslist ol on wo.OrderLineId = ol.RowId
join documents.documentbodies db on wo.DocumentLineId = db.Id
join documents.documentheaders dh on db.DocumentId = dh.Id
join logs.document_logs dl on dl.RowId = dh.DownloadId and dl.LogTime >= '2011-10-01' and dl.LogTime <= '2011-10-17';");

			int total = ds.Tables[0].Rows.Count;
			int equal = (from DataRow row in ds.Tables[0].Rows
			             let orderProductId = ((row["orderProductId"] is DBNull) ? 0 : Convert.ToInt32(row["orderProductId"]))
			             let waybillProductId = ((row["waybillProductId"] is DBNull) ? 0 : Convert.ToInt32(row["waybillProductId"]))
			             where orderProductId == waybillProductId
			             select orderProductId).Count();

			double res = (double)equal/total*100f;  // 99.8
		}

		[Test, Ignore]
		public void CodeAssignedCheck()
		{
			With.DefaultConnectionStringName = "Local";
			var ds = TestHelper.Fill(@"
SELECT oh.RowId, ol.Code orderCode, ol.ProductId orderProductId, oh.PriceCode
FROM orders.ordershead oh
inner join orders.orderslist ol on oh.RowId = ol.OrderId
where oh.WriteTime >= '2011-10-10' and oh.WriteTime <= '2011-10-17';
");

			int total = 0;
			int cnt1 = 0;
			int cnt2 = 0;

			uint prevId = 0;
			IList<TestSynonym> syns = null;

			foreach (DataRow row in ds.Tables[0].Rows)
			{
				total++;
				uint orderId = Convert.ToUInt32(row["RowId"]);
				string orderCode = ((row["orderCode"] is DBNull) ? "" : Convert.ToString(row["orderCode"]));
				uint priceCode = Convert.ToUInt32(row["PriceCode"]);

				IList<TestSynonym> tempSyns;

				if (prevId != orderId)
				{
					using (new SessionScope())
					{
						syns = TestSynonym.Queryable.Where(s => s.SupplierCode != null && s.PriceCode == priceCode).ToList();
					}
					prevId = orderId;
				}
				tempSyns = syns.Where(s => s.SupplierCode == orderCode).ToList();

				cnt1 = cnt1 + tempSyns.Count;

				foreach (var syn in tempSyns)
				{
					uint synProductId = syn.ProductId.HasValue ? syn.ProductId.Value : 0;
					int docProductId = ((row["orderProductId"] is DBNull) ? 0 : Convert.ToInt32(row["orderProductId"]));
					if (synProductId == docProductId)
						cnt2++;
				}
			}

			
			double res = (double) cnt2/cnt1*100f; // 61
		}

		[Test, Ignore]
		public void CodeAssignedCheck2()
		{
			With.DefaultConnectionStringName = "Local";
			var ds = TestHelper.Fill(@"
select dh.Id, db.Code docCode, db.ProductId docProductId, pd.PriceCode
from documents.documentheaders dh
inner join documents.documentbodies db on dh.Id = db.DocumentId
inner join usersettings.pricesdata pd on pd.FirmCode = dh.FirmCode
where dh.WriteTime >= '2011-10-10' and dh.WriteTime <= '2011-10-17';
");
			int total = 0;
			int cnt1 = 0;
			int cnt2 = 0;

			uint prevId = 0;
			IList<TestSynonym> syns = null;

			foreach (DataRow row in ds.Tables[0].Rows)
			{
				total++;
				uint docId = Convert.ToUInt32(row["Id"]);
				string docCode = ((row["docCode"] is DBNull) ? "" : Convert.ToString(row["docCode"]));
				uint priceCode = Convert.ToUInt32(row["PriceCode"]);

				IList<TestSynonym> tempSyns;

				if (prevId != docId)
				{
					using (new SessionScope())
					{
						syns = TestSynonym.Queryable.Where(s => s.SupplierCode != null && s.PriceCode == priceCode).ToList();
					}
					prevId = docId;
				}
				tempSyns = syns.Where(s => s.SupplierCode == docCode).ToList();

				cnt1 = cnt1 + tempSyns.Count;

				foreach (var syn in tempSyns)
				{
					uint synProductId = syn.ProductId.HasValue ? syn.ProductId.Value : 0;
					int docProductId = ((row["docProductId"] is DBNull) ? 0 : Convert.ToInt32(row["docProductId"]));
					if (synProductId == docProductId)
						cnt2++;
				}				
			}
			double res = (double)cnt2 / cnt1 * 100f;// 92.35
		}
	}
}
