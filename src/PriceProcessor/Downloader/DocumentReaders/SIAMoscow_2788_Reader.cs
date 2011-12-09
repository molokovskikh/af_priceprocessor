using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.PriceProcessor;
using System.Data.OleDb;
using Inforoom.Downloader.Documents;
using Inforoom.Common;
using FileHelper = Inforoom.Common.FileHelper;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;


namespace Inforoom.Downloader.DocumentReaders
{
	public class SIAMoscow_2788_Reader : BaseDocumentReader
	{
		sealed class HeaderTable
		{
			public static string colMsgNum = "MsgNum";
			public static string colBrecQ = "BrecQ";
			public static string colMsgD = "MsgD";
			public static string colMsgT = "MsgT";
			public static string colCMN = "CMN";
			public static string colObtCod = "ObtCod";
			public static string colInvNum = "InvNum";
			public static string colInvDt = "InvDt";
		}

		sealed class BodyTable
		{
			public static string colMsgNum = "MsgNum";
			public static string colItemId = "ItemId";
			public static string colLocalCod = "LocalCod";
			public static string colItemQty = "ItemQty";
			public static string colCatPrNV = "CatPrNV";
			public static string colCatTot = "CatTot";
			public static string colVAT = "VAT";
			public static string colCatVat = "CatVat";
			public static string colSeries = "Series";
			public static string colUseBefor = "UseBefor";
			public static string colSerNumID = "SerNumID";
			public static string colFirmId = "FirmId";
			public static string colLandId = "LandId";
			public static string colGCHDN = "GCHDN";
			public static string colBarCod = "BarCod";
		}

		sealed class ResultTable
		{
			public static string colDocumentID = "DocID";
			public static string colDocumentDate = "DocDate";
			public static string colBillingNumber = "BilNum";
			public static string colBillingDate = "BilDate";
			public static string colPositionID = "PosID";
			public static string colPositionName = "PosName";
			public static string colQuantity = "Quantity";
			public static string colCost = "Cost";
			public static string colStavkaNDS = "StavkaNDS";
			public static string colNdsAmount = "SummaNDS";
			public static string colCostWithNDS = "CostWNDS";
			public static string colSeria = "Seria";
			public static string colPeriod = "Period";
			public static string colCertificat = "CertID";
			public static string colProducerName = "ProdName";
			public static string colCountry = "Country";
			public static string colGCHDN = "GCHDN";
			public static string colBarCode = "BarCode";
			public static string colFirmName = "FirmName";
		}

		public SIAMoscow_2788_Reader()
		{
			excludeExtentions = new string[] { };
		}

		public override string[] UnionFiles(string[] InputFiles)
		{
			var inputList = new List<string>(InputFiles);
			var outputList = new List<string>();
			while (inputList.Count > 0)
			{
				var shortName = Path.GetFileName(inputList[0]);
				if (shortName.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase) && (shortName.StartsWith("b_", StringComparison.OrdinalIgnoreCase) || shortName.StartsWith("h_", StringComparison.OrdinalIgnoreCase)))
				{
					var doubleName = Path.GetFileNameWithoutExtension(shortName).Substring(2);
					if (shortName.StartsWith("b_", StringComparison.OrdinalIgnoreCase))
						doubleName = "h_" + doubleName + ".dbf";
					else
						doubleName = "b_" + doubleName + ".dbf";
					//������� ����� ���� � �����
					var originalDoubleName = inputList.Find(s => s.Equals(Path.GetDirectoryName(inputList[0]) + Path.DirectorySeparatorChar + doubleName, StringComparison.OrdinalIgnoreCase));

					//���� �����, �� ���������� ��� �����, ���� �� ����� � ���� ������ �����, �� ���������� ���� ����
					if (originalDoubleName != default(string))
					{
						FileHelper.ClearReadOnly(inputList[0]);
						FileHelper.ClearReadOnly(originalDoubleName);
						//todo: filecopy ����� ���������� ����������� ���������� ������ �� ��������� ����� ��� ����������� ������� ��������, ��-�� �������������, ��� ���� �������� � �������� ����������
						var archiveFileName = ArhiveFiles(new[] { inputList[0], originalDoubleName });
						outputList.Add(archiveFileName);
						File.Delete(inputList[0]);
						File.Delete(originalDoubleName);
						inputList.Remove(originalDoubleName);
					}
					else
					{
						//����� ���� � �����, �� ��� � �� ���������
						if (DateTime.Now.Subtract(File.GetLastWriteTime(inputList[0])).TotalMinutes > 6 * Settings.Default.FileDownloadInterval)
						{
							outputList.Add(ArhiveFiles(new[] { inputList[0] }));
							File.Delete(inputList[0]);
						}
					}

					inputList.RemoveAt(0);
				}
				else
				{
					//���� ��� �� ��������� ���������, �� ������ ��������� �� � �������������� ������
					outputList.Add(inputList[0]);
					inputList.RemoveAt(0);
				}
			}
			return outputList.ToArray();
		}

		public override string[] DivideFiles(string ExtractDir, string[] InputFiles)
		{
			var outputList = new List<string>();
			if (InputFiles.Length != 2)
				throw new Exception("������������ ������ ���������� : " + String.Join(", ", InputFiles));
			string headerFile = String.Empty, bodyFile = String.Empty;
			foreach (string s in InputFiles)
			{
				if (Path.GetFileName(s).StartsWith("h_", StringComparison.OrdinalIgnoreCase))
					headerFile = s;
				else
					if (Path.GetFileName(s).StartsWith("b_", StringComparison.OrdinalIgnoreCase))
						bodyFile = s;
			}
			if (String.IsNullOrEmpty(headerFile))
				throw new Exception("�� ������ ���� ��������� ��������� : " + String.Join(", ", InputFiles));
			if (String.IsNullOrEmpty(bodyFile))
				throw new Exception("�� ������ ���� ����������� ��������� : " + String.Join(", ", InputFiles));

			var FirmClientCode = Path.GetFileNameWithoutExtension(headerFile).Substring(2);

			DataTable dtHeader, dtBody;
			try
			{
				dtHeader = Dbf.Load(headerFile);
				dtHeader.TableName = "Header";
			}
			catch (Exception exDBF)
			{
				throw new Exception("���������� ������� ���� ��������� ���������.", exDBF);
			}
			try
			{
				dtBody = Dbf.Load(bodyFile);
				dtBody.TableName = "Body";
			}
			catch (Exception exDBF)
			{
				throw new Exception("���������� ������� ���� ����������� ���������.", exDBF);
			}

			var dsSource = new DataSet();
			DataSet dsStandaloneDocument;
			dsSource.Tables.Add(dtHeader);
			dsSource.Tables.Add(dtBody);
			foreach (DataRow drHeader in dtHeader.Rows)
			{
				string DeliveryCode = drHeader[HeaderTable.colObtCod].ToString(),
					OrderID = drHeader[HeaderTable.colCMN].ToString();

				var documentFileName = Path.Combine(ExtractDir, String.Format("{0}_{1}_{2}_{3}.xml", 
					FirmClientCode, DeliveryCode, OrderID, drHeader[HeaderTable.colMsgNum]));

				dsStandaloneDocument = dsSource.Clone();
				var drTemp = dsStandaloneDocument.Tables["Header"].NewRow();
				drTemp.ItemArray = drHeader.ItemArray;
				dsStandaloneDocument.Tables["Header"].Rows.Add(drTemp);
				foreach (var drBody in dtBody.Select("MsgNum = " + drHeader[HeaderTable.colMsgNum]))
				{
					drTemp = dsStandaloneDocument.Tables["Body"].NewRow();
					drTemp.ItemArray = drBody.ItemArray;
					dsStandaloneDocument.Tables["Body"].Rows.Add(drTemp);
				}
				dsStandaloneDocument.WriteXml(documentFileName, XmlWriteMode.WriteSchema);
				outputList.Add(documentFileName);
			}

			return outputList.ToArray();
		}

		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			var list = new List<ulong>();		

            var SQL = SqlGetClientAddressId(true, true) +
                Environment.NewLine + GetFilterSQLFooter();

			string FirmClientCode, DeliveryCode;
			try
			{
				string[] parts = Path.GetFileNameWithoutExtension(CurrentFileName).Split('_');
				FirmClientCode = parts[0];
				DeliveryCode = parts[1];
			}
			catch (Exception ex)
			{
				throw new Exception("�� ���������� ������������ SupplierClientId(FirmClientCode) � SupplierDeliveryId(FirmClientCode2) �� ���������.", ex);
			}

			var ds = MySqlHelper.ExecuteDataset(
				Connection,
				SQL,
				new MySqlParameter("?SupplierId", FirmCode),
				new MySqlParameter("?SupplierClientId", FirmClientCode),
				new MySqlParameter("?SupplierDeliveryId", DeliveryCode));

			foreach (DataRow drApteka in ds.Tables[0].Rows)
				list.Add(Convert.ToUInt64(drApteka["AddressId"]));

			if (list.Count == 0)
				throw new Exception("�� ������� ����� �������� � SupplierClientId(FirmClientCode) = " + FirmClientCode + 
					" � SupplierDeliveryId(FirmClientCode2) = " + DeliveryCode + ".");

			return list;
		}

		//�������������� �����
		private static string ArhiveFiles(string[] InputFiles)
		{
			var zipFileName = Path.GetDirectoryName(InputFiles[0]) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(InputFiles[0]).Substring(2) + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".zip";

			using (var s = new ZipOutputStream(File.Create(zipFileName)))
			{
				s.SetLevel(5); // 0 - store only to 9 - means best compression

				foreach (var file in InputFiles)
				{
					var buffer = File.ReadAllBytes(file);
					var entry = new ZipEntry(Path.GetFileName(file));

					s.PutNextEntry(entry);

					s.Write(buffer, 0, buffer.Length);
				}
				s.Finish();
			}


			File.SetLastWriteTime(zipFileName, DateTime.Now.AddMinutes(-2 * Settings.Default.FileDownloadInterval));
			return zipFileName;
		}

		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			string outFile = Path.GetDirectoryName(InputFile) + Path.DirectorySeparatorChar + "out.dbf";
			if (File.Exists(outFile))
				File.Delete(outFile);

			var dsDocument = new DataSet();
			dsDocument.ReadXml(InputFile);
			if (Convert.ToInt32(dsDocument.Tables["Header"].Rows[0][HeaderTable.colBrecQ]) != dsDocument.Tables["Body"].Rows.Count)
				throw new Exception("���������� ������� � ��������� �� ������������� �������� � ��������� ��������� � " + HeaderTable.colMsgNum + " = " + dsDocument.Tables["Header"].Rows[0][HeaderTable.colMsgNum].ToString());

			var dtResult = GetResultTable();

			var drHeader = dsDocument.Tables["Header"].Rows[0];
			foreach (DataRow drBody in dsDocument.Tables["Body"].Rows)
			{
				var drResult = dtResult.NewRow();
				drResult[ResultTable.colDocumentID] = Convert.ToString(drHeader[HeaderTable.colInvNum]);
				drResult[ResultTable.colDocumentDate] = Convert.ToDateTime(drHeader[HeaderTable.colInvDt]);
				//��� ��� ���� �� �����������, �.�. ��� ������ �� ����� �� ��������� � �������� �������
				//drResult[ResultTable.colBillingNumber] = drHeader[HeaderTable.colInvNum];
				//drResult[ResultTable.colBillingDate] = Convert.ToDateTime(drHeader[HeaderTable.colInvDt]);
				drResult[ResultTable.colFirmName] = drSource[WaybillSourcesTable.colShortName];


				drResult[ResultTable.colPositionID] = drBody[BodyTable.colLocalCod];
				drResult[ResultTable.colPositionName] = drBody[BodyTable.colItemId];
				drResult[ResultTable.colQuantity] = Convert.ToInt32(drBody[BodyTable.colItemQty]);
				drResult[ResultTable.colCost] = Convert.ToDecimal(drBody[BodyTable.colCatPrNV], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				drResult[ResultTable.colStavkaNDS] = Convert.ToDecimal(drBody[BodyTable.colVAT], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				drResult[ResultTable.colNdsAmount] = Convert.ToDecimal(drBody[BodyTable.colCatVat], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				drResult[ResultTable.colCostWithNDS] = Convert.ToDecimal(drBody[BodyTable.colCatTot], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				drResult[ResultTable.colSeria] = drBody[BodyTable.colSeries];
				drResult[ResultTable.colPeriod] = Convert.ToDateTime(drBody[BodyTable.colUseBefor]);
				drResult[ResultTable.colCertificat] = drBody[BodyTable.colSerNumID];
				drResult[ResultTable.colProducerName] = drBody[BodyTable.colFirmId];
				drResult[ResultTable.colCountry] = drBody[BodyTable.colLandId];
				drResult[ResultTable.colGCHDN] = drBody[BodyTable.colGCHDN];
				drResult[ResultTable.colBarCode] = drBody[BodyTable.colBarCod];

				dtResult.Rows.Add(drResult);
			}

			using (var file = new StreamWriter(File.Create(outFile), Encoding.GetEncoding(866)))
			{
				Dbf.Save(dtResult, file);
			}

			return outFile;
		}

		private static DataTable GetResultTable()
		{
			var dtResult = new DataTable("Result");
			dtResult.Columns.Add(ResultTable.colDocumentID, typeof(string));
			dtResult.Columns.Add(ResultTable.colDocumentDate, typeof(DateTime));
			dtResult.Columns.Add(ResultTable.colBillingNumber, typeof(string));
			dtResult.Columns[ResultTable.colBillingNumber].MaxLength = 20;
			dtResult.Columns.Add(ResultTable.colBillingDate, typeof(DateTime));
			dtResult.Columns.Add(ResultTable.colPositionID, typeof(string));
			dtResult.Columns[ResultTable.colPositionID].MaxLength = 20;
			dtResult.Columns.Add(ResultTable.colPositionName, typeof(string));
			dtResult.Columns[ResultTable.colPositionName].MaxLength = 120;
			dtResult.Columns.Add(ResultTable.colQuantity, typeof(int));
			dtResult.Columns.Add(ResultTable.colCost, typeof(decimal));
			dtResult.Columns.Add(ResultTable.colStavkaNDS, typeof(decimal));
			dtResult.Columns.Add(ResultTable.colNdsAmount, typeof(decimal));
			dtResult.Columns.Add(ResultTable.colCostWithNDS, typeof(decimal));
			dtResult.Columns.Add(ResultTable.colSeria, typeof(string));
			dtResult.Columns[ResultTable.colSeria].MaxLength = 120;
			dtResult.Columns.Add(ResultTable.colPeriod, typeof(DateTime));
			dtResult.Columns.Add(ResultTable.colCertificat, typeof(string));
			dtResult.Columns[ResultTable.colCertificat].MaxLength = 50;
			dtResult.Columns.Add(ResultTable.colProducerName, typeof(string));
			dtResult.Columns[ResultTable.colProducerName].MaxLength = 32;
			dtResult.Columns.Add(ResultTable.colCountry, typeof(string));
			dtResult.Columns[ResultTable.colCountry].MaxLength = 32;
			dtResult.Columns.Add(ResultTable.colGCHDN, typeof(string));
			dtResult.Columns[ResultTable.colGCHDN].MaxLength = 32;
			dtResult.Columns.Add(ResultTable.colBarCode, typeof(string));
			dtResult.Columns[ResultTable.colBarCode].MaxLength = 13;
			dtResult.Columns.Add(ResultTable.colFirmName, typeof(string));
			dtResult.Columns[ResultTable.colFirmName].MaxLength = 50;
			return dtResult;
		}

		public override void ImportDocument(DocumentReceiveLog log, string filename)
		{
			var dsDocument = new DataSet();
			dsDocument.ReadXml(filename);
			var providerDocumentId = dsDocument.Tables["Header"].Rows[0][HeaderTable.colInvNum].ToString();

			using(new SessionScope())
			{
				var doc = Document.Queryable.FirstOrDefault(d => d.FirmCode == log.Supplier.Id
					&& d.ClientCode == log.ClientCode
					&& d.ProviderDocumentId == providerDocumentId
					&& d.DocumentType == log.DocumentType);
				if (doc != null)
					throw new Exception(String.Format(
							"������������� �������� � �����������: FirmCode = {0}, ClientCode = {1}, DocumentType = {2}, ProviderDocumentId = {3}",
							log.Supplier.Id,
							log.ClientCode,
							log.DocumentType,
							providerDocumentId));
				doc = new Document(log) {
					ProviderDocumentId = providerDocumentId
				};
				using(var transaction = new TransactionScope(OnDispose.Rollback))
				{
					log.Save();
					doc.Save();
					transaction.VoteCommit();
				}
			}
		}
	}
}
