using System;
using System.Data;
using System.IO;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Models;
using log4net;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	public class DbfExporter
	{
		private static ILog _log = LogManager.GetLogger(typeof(DbfExporter));

		public static bool ConvertAndSaveDbfFormatIfNeeded(Document document)
		{
			if (!document.SetAssortimentInfo())
				return false;
			var path = String.Empty;
			var log = document.Log;
			try
			{
				DocumentReceiveLog convertedLog;
				//если название файла не задано то значит документ пришел из сервиса протека и у него нет файла
				//в этом случае нет смысла делать дополнительный лог загрузки
				if (String.IsNullOrEmpty(document.Log.FileName))
				{
					convertedLog = log;
					convertedLog.FileName = convertedLog.Id + ".dbf";
					convertedLog.IsFake = false;
				}
				else
				{
					convertedLog = new DocumentReceiveLog(log);
					log.IsFake = true;
				}

				convertedLog.SaveAndFlush();

				//сохраняем накладную в новом формате dbf.
				var table = InitTableForFormatDbf(document);
				Dbf.Save(table, convertedLog.GetRemoteFileNameExt());
			}
			catch (Exception e)
			{
				var info = String.Format("Исходный файл: {0} , log.id = {1}. Сконвертированный: {2}. ClientCode = {3}. SupplierId = {4}.", log.FileName, log.Id, path, log.ClientCode, log.Supplier.Id);
				_log.Error("Ошибка сохранения накладной в новый формат dbf. "+ info, e);
				throw;
			}
			return true;
		}

		private static DataTable InitTableForFormatDbf(Document document)
		{
			var table = new DataTable();

			table.Columns.AddRange(new DataColumn[] {
				new DataColumn("postid_af"),
				new DataColumn("post_name_af"),
				new DataColumn("apt_af"),
				new DataColumn("aname_af"),
				new DataColumn("ttn"),
				new DataColumn("ttn_date"),
				new DataColumn("id_artis"),
				new DataColumn("name_artis"),
				new DataColumn("przv_artis"),
				new DataColumn("name_post"),
				new DataColumn("przv_post"),
				new DataColumn("seria"),
				new DataColumn("sgodn"),
				new DataColumn("sert"),
				new DataColumn("sert_date"),
				new DataColumn("prcena_bnds"),
				new DataColumn("gr_cena"),
				new DataColumn("pcena_bnds"),
				new DataColumn("nds"),
				new DataColumn("pcena_nds"),
				new DataColumn("kol_tov"),
				new DataColumn("ean13")
			});

			foreach (var line in document.Lines)
			{
				var row = table.NewRow();
				row["postid_af"] = document.FirmCode;
				row["post_name_af"] = document.Log.Supplier.FullName;
				row["apt_af"] = document.Address.Id;
				row["aname_af"] = document.Address.Name;
				row["ttn"] = document.ProviderDocumentId;
				row["ttn_date"] = document.DocumentDate;
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.Code != null)
					row["id_artis"] = line.AssortimentPriceInfo.Code;
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.Synonym != null)
					row["name_artis"] = line.AssortimentPriceInfo.Synonym;
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.SynonymFirmCr != null)
					row["przv_artis"] = line.AssortimentPriceInfo.SynonymFirmCr;
				row["name_post"] = line.Product;
				row["przv_post"] = line.Producer;
				row["seria"] = line.SerialNumber;
				row["sgodn"] = line.Period;
				row["sert"] = line.Certificates;
				row["sert_date"] = line.CertificatesDate;
				row["prcena_bnds"] = line.ProducerCostWithoutNDS;
				row["gr_cena"] = line.RegistryCost;
				row["pcena_bnds"] = line.SupplierCostWithoutNDS;
				row["nds"] = line.Nds;
				row["pcena_nds"] = line.SupplierCost;
				row["kol_tov"] = line.Quantity;
				row["ean13"] = line.EAN13;

				table.Rows.Add(row);
			}

			return table;
		}

		public static void SaveProtek(Document document)
		{
			var table = new DataTable();

			table.Columns.AddRange(new DataColumn[] {
				new DataColumn("ID"),
				new DataColumn("Date", typeof(DateTime)),
				new DataColumn("Address"),
				new DataColumn("Org"),
				new DataColumn("SupID"),
				new DataColumn("SupDate", typeof(DateTime)),
				new DataColumn("Product"),
				new DataColumn("Code"),
				new DataColumn("Cert"),
				new DataColumn("CertDate"),
				new DataColumn("Period"),
				new DataColumn("Producer"),
				new DataColumn("Country"),
				new DataColumn("ProdCost", typeof(decimal)),
				new DataColumn("RegCost", typeof(decimal)),
				new DataColumn("CostMarkup", typeof(decimal)),
				new DataColumn("CostWoNds", typeof(decimal)),
				new DataColumn("CostWNds", typeof(decimal)),
				new DataColumn("Quantity", typeof(int)),
				new DataColumn("VI", typeof(bool)),
				new DataColumn("Nds", typeof(int)),
				new DataColumn("Seria"),
				new DataColumn("Sum", typeof(decimal)),
				new DataColumn("NdsSum", typeof(decimal)),
				new DataColumn("Unit"),
				new DataColumn("ExciseTax", typeof(decimal)),
				new DataColumn("Entry"),
				new DataColumn("Ean13")
			});

			foreach (var line in document.Lines)
			{
				var row = table.NewRow();
				row.SetField("ID", document.Log.Id);
				row.SetField("Date", document.Log.LogTime);
				row.SetField("Address", document.Address.Name);
				row.SetField("Org", document.Address.Org.FullName);
				row.SetField("SupID", document.ProviderDocumentId);
				row.SetField("SupDate", document.DocumentDate);
				row.SetField("Product", line.Product);
				row.SetField("Code", line.Code);
				row.SetField("Cert", line.Certificates);
				row.SetField("CertDate", line.CertificatesDate);
				row.SetField("Period", line.Period);
				row.SetField("Producer", line.Producer);
				row.SetField("Country", line.Country);
				row.SetField("ProdCost", line.ProducerCost);
				row.SetField("RegCost", line.RegistryCost);
				row.SetField("CostMarkup", line.SupplierPriceMarkup);
				row.SetField("CostWoNds", line.SupplierCostWithoutNDS);
				row.SetField("CostWNds", line.SupplierCost);
				row.SetField("Quantity", line.Quantity);
				row.SetField("VI", line.VitallyImportant);
				row.SetField("Nds", line.Nds);
				row.SetField("Seria", line.SerialNumber);
				row.SetField("Sum", line.Amount);
				row.SetField("NdsSum", line.NdsAmount);
				row.SetField("Unit", line.Unit);
				row.SetField("ExciseTax", line.ExciseTax);
				row.SetField("Entry", line.BillOfEntryNumber);
				row.SetField("Ean13", line.EAN13);
				table.Rows.Add(row);
			}

			document.Log.IsFake = false;
			var id = document.ProviderDocumentId;
			if (string.IsNullOrEmpty(id))
				id = document.Log.Id.ToString();

			document.Log.FileName = id + ".dbf";
			var filename = document.Log.GetRemoteFileNameExt();
			Dbf.Save(table, filename);
			document.Log.DocumentSize = new FileInfo(filename).Length;
		}
	}
}