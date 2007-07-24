using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;
using ICSharpCode.SharpZipLib.Zip;
using Inforoom.Downloader.Properties;
using System.Data.OleDb;
using Inforoom.Downloader.Documents;

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
			public static string colSummaNDS = "SummaNDS";
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
			List<string> inputList = new List<string>(InputFiles);
			List<string> outputList = new List<string>();
			while (inputList.Count > 0)
			{
				string shortName = Path.GetFileName(inputList[0]);
				if (shortName.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase) && (shortName.StartsWith("b_", StringComparison.OrdinalIgnoreCase) || shortName.StartsWith("h_", StringComparison.OrdinalIgnoreCase)))
				{
					string doubleName = Path.GetFileNameWithoutExtension(shortName).Substring(2);
					if (shortName.StartsWith("b_", StringComparison.OrdinalIgnoreCase))
						doubleName = "h_" + doubleName + ".dbf";
					else
						doubleName = "b_" + doubleName + ".dbf";
					//Попытка найти пару к файлу
					string originalDoubleName = inputList.Find(delegate(string s) { return s.Equals(Path.GetDirectoryName(inputList[0]) + Path.DirectorySeparatorChar + doubleName, StringComparison.OrdinalIgnoreCase); });

					//Если нашли, то архивируем оба файла, если не нашли и файл создан давно, то архивируем один файл
					if (originalDoubleName != default(string))
					{
						outputList.Add(ArhiveFiles(new string[] { inputList[0], originalDoubleName }));
						File.Delete(inputList[0]);
						File.Delete(originalDoubleName);
						inputList.Remove(originalDoubleName);
					}
					else
					{
						//Ждали пару к файлу, но так и не дождались
						if (DateTime.Now.Subtract(File.GetLastWriteTime(inputList[0])).TotalMinutes > 6 * Settings.Default.FileDownloadInterval)
						{
							outputList.Add(ArhiveFiles(new string[] { inputList[0]}));
							File.Delete(inputList[0]);
						}
					}

					inputList.RemoveAt(0);
				}
				else
				{
					//Если это не документы накладных, то просто добавляем их в результирующий список
					outputList.Add(inputList[0]);
					inputList.RemoveAt(0);
				}
			}
			return outputList.ToArray();
		}

		public override string[] DivideFiles(string ExtractDir, string[] InputFiles)
		{
			string FirmClientCode;
			List<string> outputList = new List<string>();
			if (InputFiles.Length != 2)
				throw new Exception("Некорректный список документов : " + String.Join(", ", InputFiles));
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
				throw new Exception("Не найден файл заголовка документа : " + String.Join(", ", InputFiles));
			if (String.IsNullOrEmpty(bodyFile))
				throw new Exception("Не найден файл содержимого документа : " + String.Join(", ", InputFiles));

			FirmClientCode = Path.GetFileNameWithoutExtension(headerFile).Substring(2);

			DataTable dtHeader, dtBody;
			try
			{
				dtHeader = ReadDBF(headerFile);
				dtHeader.TableName = "Header";
			}
			catch (Exception exDBF)
			{
				throw new Exception("Невозможно открыть файл заголовка документа.", exDBF);
			}
			try
			{
				dtBody = ReadDBF(bodyFile);
				dtBody.TableName = "Body";
			}
			catch (Exception exDBF)
			{
				throw new Exception("Невозможно открыть файл содержимого документа.", exDBF);
			}

			DataSet dsSource = new DataSet();
			DataSet dsStandaloneDocument;
			dsSource.Tables.Add(dtHeader);
			dsSource.Tables.Add(dtBody);
			foreach (DataRow drHeader in dtHeader.Rows)
			{
				string DeliveryCode = drHeader[HeaderTable.colObtCod].ToString(),
					OrderID = drHeader[HeaderTable.colCMN].ToString();

				string documentFileName = ExtractDir + String.Format("{0}_{1}_{2}_{3}.xml", FirmClientCode, DeliveryCode, OrderID, drHeader[HeaderTable.colMsgNum].ToString());

				dsStandaloneDocument = dsSource.Clone();
				DataRow drTemp = dsStandaloneDocument.Tables["Header"].NewRow();
				drTemp.ItemArray = drHeader.ItemArray;
				dsStandaloneDocument.Tables["Header"].Rows.Add(drTemp);
				foreach (DataRow drBody in dtBody.Select("MsgNum = " + drHeader[HeaderTable.colMsgNum].ToString()))
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
			List<ulong> list = new List<ulong>();
			string SQL = GetFilterSQLHeader() + Environment.NewLine + "and i.FirmClientCode = ?FirmClientCode and i.FirmClientCode2 = ?DeliveryCode " + Environment.NewLine + GetFilterSQLFooter();

			string FirmClientCode, DeliveryCode;
			try
			{
				string[] parts = Path.GetFileNameWithoutExtension(CurrentFileName).Split('_');
				FirmClientCode = parts[0];
				DeliveryCode = parts[1];
			}
			catch (Exception ex)
			{
				throw new Exception("Не получилось сформировать FirmClientCode и FirmClientCode2 из документа.", ex);
			}

			DataSet ds = MySqlHelper.ExecuteDataset(
				Connection,
				SQL,
				new MySqlParameter("?FirmCode", FirmCode),
				new MySqlParameter("?FirmClientCode", FirmClientCode),
				new MySqlParameter("?DeliveryCode", DeliveryCode));

			foreach (DataRow drApteka in ds.Tables[0].Rows)
				list.Add(Convert.ToUInt64(drApteka["ClientCode"]));

			if (list.Count == 0)
				throw new Exception("Не удалось найти клиентов с FirmClientCode = " + FirmClientCode + " и FirmClientCode2 = " + DeliveryCode + ".");

			return list;
		}

		//заархивировать файлы
		string ArhiveFiles(string[] InputFiles)
		{
			string zipFileName = Path.GetDirectoryName(InputFiles[0]) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(InputFiles[0]).Substring(2) + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".zip";

			ZipOutputStream s = new ZipOutputStream(File.Create(zipFileName));

			s.SetLevel(5); // 0 - store only to 9 - means best compression

			foreach (string file in InputFiles)
			{
				FileStream fs = File.OpenRead(file);

				byte[] buffer = new byte[fs.Length];
				fs.Read(buffer, 0, buffer.Length);
				fs.Close();
				fs.Dispose();
				fs = null;

				ZipEntry entry = new ZipEntry(Path.GetFileName( file ));

				s.PutNextEntry(entry);

				s.Write(buffer, 0, buffer.Length);

			}

			s.Finish();
			s.Close();

			File.SetLastWriteTime(zipFileName, DateTime.Now.AddMinutes(-2 * Settings.Default.FileDownloadInterval));

			return zipFileName;
		}

		DataTable ReadDBF(string InputFile)
		{
			OleDbConnection dbcMain = new OleDbConnection(String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"dBase 5.0\"", System.IO.Path.GetDirectoryName(InputFile)));
			dbcMain.Open();
			try
			{
				DataTable dtFile = new DataTable();
				string newFileName = Path.GetDirectoryName(InputFile) + Path.DirectorySeparatorChar + "inp.dbf";
				if (File.Exists(newFileName))
					File.Delete(newFileName);
				File.Copy(InputFile, newFileName);
				OleDbDataAdapter da = new OleDbDataAdapter(String.Format("select * from [{0}]", System.IO.Path.GetFileNameWithoutExtension(newFileName)), dbcMain);
				da.Fill(dtFile);
				return dtFile;
			}
			finally
			{
				dbcMain.Close();
				dbcMain.Dispose();
				dbcMain = null;
			} 
		}

		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			string outFile = Path.GetDirectoryName(InputFile) + Path.DirectorySeparatorChar + "out.dbf";
			if (File.Exists(outFile))
				File.Delete(outFile);

			DataSet dsDocument = new DataSet();
			dsDocument.ReadXml(InputFile);
			if (Convert.ToInt32(dsDocument.Tables["Header"].Rows[0][HeaderTable.colBrecQ]) != dsDocument.Tables["Body"].Rows.Count)
				throw new Exception("Количество позиций в документе не соответствует значению в заголовке документа с " + HeaderTable.colMsgNum + " = " + dsDocument.Tables["Header"].Rows[0][HeaderTable.colMsgNum].ToString());

			DataTable dtResult = GetResultTable();
			DataRow drResult;

			DataRow drHeader = dsDocument.Tables["Header"].Rows[0];
			foreach (DataRow drBody in dsDocument.Tables["Body"].Rows)
			{
				drResult = dtResult.NewRow();
                drResult[ResultTable.colDocumentID] = Convert.ToString(drHeader[HeaderTable.colInvNum]);
                //drResult[ResultTable.colDocumentDate] = Convert.ToDateTime(drHeader[HeaderTable.colMsgD]).Add(TimeSpan.Parse(drHeader[HeaderTable.colMsgT].ToString()));
                drResult[ResultTable.colDocumentDate] = Convert.ToDateTime(drHeader[HeaderTable.colInvDt]);
                //drResult[ResultTable.colBillingNumber] = drHeader[HeaderTable.colInvNum];
                //drResult[ResultTable.colBillingDate] = Convert.ToDateTime(drHeader[HeaderTable.colInvDt]);
                //drResult[ResultTable.colBillingDate] = Convert.ToDateTime(drHeader[HeaderTable.colInvDt]);
				drResult[ResultTable.colFirmName] = drSource[WaybillSourcesTable.colShortName];


				drResult[ResultTable.colPositionID] = drBody[BodyTable.colLocalCod];
				drResult[ResultTable.colPositionName] = drBody[BodyTable.colItemId];
				drResult[ResultTable.colQuantity] = Convert.ToInt32(drBody[BodyTable.colItemQty]);
				drResult[ResultTable.colCost] = Convert.ToDecimal(drBody[BodyTable.colCatPrNV], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				drResult[ResultTable.colStavkaNDS] = Convert.ToDecimal(drBody[BodyTable.colVAT], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				drResult[ResultTable.colSummaNDS] = Convert.ToDecimal(drBody[BodyTable.colCatVat], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
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

			DataTableToDbf(outFile, dtResult, false);

			return outFile;
		}

		DataTable GetResultTable()
		{
			DataTable dtResult = new DataTable("Result");
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
			dtResult.Columns.Add(ResultTable.colSummaNDS, typeof(decimal));
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

		void DataTableToDbf(string fileName, DataTable dataTable, bool useFoxProFriver)
		{

			string tableName = Path.GetFileNameWithoutExtension(fileName);
			string dbfPath = Path.GetDirectoryName(fileName);
			string connectionString;

			if (String.IsNullOrEmpty(dbfPath))
				dbfPath = ".";

			if (useFoxProFriver)
				connectionString = "Provider=VFPOLEDB;Data Source={0};";
			else
				connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=dBase III";

			using (OleDbConnection connection = new OleDbConnection(String.Format(connectionString, dbfPath)))
			{
				StringBuilder sqlBuilder = new StringBuilder();

				if (useFoxProFriver)
					sqlBuilder.AppendFormat("CREATE DBF {0} (", tableName);
				else
					sqlBuilder.AppendFormat("CREATE TABLE {0} (", tableName);

				int i = 0;
				foreach (DataColumn column in dataTable.Columns)
				{
					sqlBuilder.Append(String.Format("[{0}]", column.ColumnName));
					if (column.DataType == typeof(int))
						sqlBuilder.Append(" int");
					else if (column.DataType == typeof(decimal))
					{
						sqlBuilder.Append(" numeric");
						if (column.ExtendedProperties.ContainsKey("Precision"))
						{
							int scale = column.ExtendedProperties.ContainsKey("Scale") ? (int)column.ExtendedProperties["Scale"] : 0;
							sqlBuilder.Append(String.Format("({0}, {1})",
								column.ExtendedProperties["Precision"],
								scale));
						}
					}
					else if (column.DataType == typeof(double))
						sqlBuilder.Append(" currency");
					else if (column.DataType == typeof(float))
						sqlBuilder.Append(" float");
					else if (column.DataType == typeof(DateTime))
						sqlBuilder.Append(" datetime");
					else if (column.DataType == typeof(bool))
						sqlBuilder.Append(" logical");
					else if (column.DataType == typeof(string) && column.MaxLength > -1 && column.MaxLength <= 255)
						sqlBuilder.Append(String.Format(" char({0})", column.MaxLength));
					else if (column.DataType == typeof(string) && column.MaxLength == -1)
						sqlBuilder.Append(" char(254)");
					else
						sqlBuilder.Append(" memo");

					if (i != dataTable.Columns.Count - 1)
						sqlBuilder.Append(",");
					sqlBuilder.AppendLine();
					i++;
				}
				sqlBuilder.Append(");");
				OleDbCommand command = new OleDbCommand(sqlBuilder.ToString(), connection);
				connection.Open();
				command.ExecuteNonQuery();
				OleDbDataAdapter adapter = new OleDbDataAdapter(String.Format("select * from [{0}]", tableName), connection);
				OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder(adapter);
				commandBuilder.QuotePrefix = "[";
				commandBuilder.QuoteSuffix = "]";
				adapter.Update(dataTable);
			}

			if (!useFoxProFriver)
			{
				//это хак для того что бы обойти ограничение на 8 символов в имени файла
				if (tableName.Length > 8)
					File.Move(String.Format("{0}\\{1}.dbf", dbfPath, tableName.Substring(0, 8)), fileName);
				else
					File.Move(Path.ChangeExtension(fileName, "dbf"), fileName);
			}
		}
	}
}
