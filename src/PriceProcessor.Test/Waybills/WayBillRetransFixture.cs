using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Waybills;
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

		[Test]	
		[Ignore]
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

		[Test]
		[Ignore]
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
	}
}
