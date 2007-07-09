using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using Inforoom.Downloader.Properties;
using ExecuteTemplate;

namespace Inforoom.Downloader
{
	class WaybillLANSourceHandler : BaseSourceHandler
	{
		public WaybillLANSourceHandler()
			: base()
		{
			this.sourceType = "WAYBILLLAN";
		}

		protected override string GetSQLSources()
		{
			return String.Format(@"
SELECT
  cd.FirmCode,
  cd.ShortName,
  st.EMailFrom
FROM
           {1}             as st
INNER JOIN {0} AS CD ON CD.FirmCode = st.FirmCode
WHERE
cd.FirmStatus   = 1
and st.SourceID = 4",
				Settings.Default.tbClientsData,
				Settings.Default.tbWaybillSources);
		}

		protected override void ProcessData()
		{
			//����� ����� ������� ����������
			DataRow drLanSource;
			string[] files;
			FillSourcesTable();
			while (dtSources.Rows.Count > 0)
			{
				drLanSource = null;
				try
				{
					drLanSource = dtSources.Rows[0];

					//�������� ������ ������ �� �����
					files = GetFileFromSource();

					foreach (string SourceFileName in files)
					{
						GetCurrentFile(SourceFileName);

						if (!String.IsNullOrEmpty(CurrFileName))
							if (CheckClientCode(Path.GetFileName(CurrFileName), Convert.ToInt32(drLanSource[SourcesTable.colFirmCode])))
							{
								bool CorrectArchive = true;
								//�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
								if (ArchiveHlp.IsArchive(CurrFileName))
								{
									if (ArchiveHlp.TestArchive(CurrFileName))
									{
										try
										{
											ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
										}
										catch (ArchiveHlp.ArchiveException)
										{
											CorrectArchive = false;
										}
									}
									else
										CorrectArchive = false;
								}

								if (CorrectArchive)
								{
									ProcessWaybillFile(CurrFileName, drLanSource, Path.GetFileName(CurrFileName).Split('_')[0]);
									//����� ��������� ����� ������� ��� �� �����
									if (!String.IsNullOrEmpty(SourceFileName) && File.Exists(SourceFileName))
										File.Delete(SourceFileName);
								}
								else
								{
									WriteLog(Convert.ToInt32(drLanSource[SourcesTable.colFirmCode]), 0, Path.GetFileName(CurrFileName), String.Format("�� ������� ����������� ���� '{0}'", Path.GetFileName(CurrFileName)));
									//����������� ���� �� �������, ������� ������� ��� �� �����
									if (!String.IsNullOrEmpty(SourceFileName) && File.Exists(SourceFileName))
										File.Delete(SourceFileName);
								}
								DeleteCurrFile();
							}
							else
							{
								DeleteCurrFile();
								WriteLog(Convert.ToInt32(drLanSource[SourcesTable.colFirmCode]), 0, Path.GetFileName(CurrFileName), String.Format("�� ����� ������� � ��������� FirmClientCode '{0}'", Path.GetFileName(CurrFileName)));
								LoggingToService(String.Format("�� ����� ������� � ��������� FirmClientCode '{0}' ��� ���������� {1} ({2})", Path.GetFileName(CurrFileName), drLanSource[SourcesTable.colShortName], drLanSource[SourcesTable.colFirmCode]));
								//����������� � �������� ���� �� �������, ������� ������� ��� �� �����, ��� �� �� �����
								if (!String.IsNullOrEmpty(SourceFileName) && File.Exists(SourceFileName))
									File.Delete(SourceFileName);
							}

					}

					drLanSource.Delete();
					dtSources.AcceptChanges();
				}
				catch (Exception ex)
				{
					string Error = String.Empty;
					Error = String.Format("�������� : {0}", dtSources.Rows[0][SourcesTable.colFirmCode]);
					try
					{
						dtSources.Rows[0].Delete();
					}
					catch { }
					Error += Environment.NewLine + Environment.NewLine + ex.ToString();
					LoggingToService(Error);
					try
					{
						dtSources.AcceptChanges();
					}
					catch { }
				}
			}		
		}

		protected string[] GetFileFromSource()
		{
			string PricePath = String.Empty;
			try
			{
				PricePath = NormalizeDir(Settings.Default.FTPOptBox) + Path.DirectorySeparatorChar + dtSources.Rows[0]["FirmCode"].ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + "Waybills";
				string[] ff = Directory.GetFiles(PricePath);
				return ff;
			}
			catch (Exception exDir)
			{
				LoggingToService(String.Format("�� ������� �������� ������ ������ ��� ����� {0}: {1}", PricePath, exDir));
				return new string[] { };
			}
		}

		private void GetCurrentFile(string sourceFile)
		{
			CurrFileName = String.Empty;
			string NewFile = DownHandlerPath + Path.GetFileName(sourceFile);
			try
			{
				if (File.Exists(NewFile))
					File.Delete(NewFile);
				File.Copy(sourceFile, NewFile);
				CurrFileName = NewFile;
			}
			catch (Exception ex)
			{
				LoggingToService(String.Format("�� ������� ����������� ���� {0}({1}) : {2}", sourceFile, System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
			}
		}

		bool CheckClientCode(string FileName, int FirmCode)
		{
			string FirmClientCode;
			if (FileName.Contains("_"))
			{
				FirmClientCode = FileName.Split('_')[0];
				if (cWork.State != ConnectionState.Open)
				{
					cWork.Open();
				}
				try
				{
					object res = MySqlHelper.ExecuteScalar(cWork, @"
SELECT
  cd.firmcode as ClientCode,
  IncludeRegulation.IncludeType,
  i.FirmClientCode
FROM
  (usersettings.clientsdata as cd,
   usersettings.intersection i,
   usersettings.pricesdata pd)
   LEFT JOIN usersettings.includeregulation
        ON includeclientcode= cd.firmcode
WHERE
  i.clientCode = cd.FirmCode
  and i.PriceCode = pd.PriceCode
  and pd.FirmCode = ?FirmCode
  and if(IncludeRegulation.PrimaryClientCode is null, 1, IncludeRegulation.IncludeType <>2)
  and i.FirmClientCode = ?FirmClientCode
group by cd.firmcode
", new MySqlParameter("?FirmCode", FirmCode), new MySqlParameter("?FirmClientCode", FirmClientCode));
					return res != null;
				}
				finally
				{
					try { cWork.Close(); }
					catch { }
				}
			}
			else
				return false;
		}

		protected void ProcessWaybillFile(string InFile, DataRow drCurrent, string FirmClientCode)
		{
			//������ ������ 
			string[] Files = new string[] { InFile };
			if (ArchiveHlp.IsArchive(InFile))
			{
				Files = Directory.GetFiles(InFile + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
			}
			foreach (string s in Files)
			{
				MoveWaybill(s, drCurrent, FirmClientCode);
			}
		}

		protected void MoveWaybill(string FileName, DataRow drCurrent, string FirmClientCode)
		{
			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(
				new ExecuteArgs(),
				delegate(ExecuteArgs args)
				{
					MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_receive_logs (FirmCode, ClientCode, FileName, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, 1); select last_insert_id();", cWork);
					cmdInsert.Parameters.Add("?FirmCode", drCurrent[SourcesTable.colFirmCode]);
					cmdInsert.Parameters.Add("?ClientCode", DBNull.Value);
					cmdInsert.Parameters.Add("?FileName", Path.GetFileName(FileName));

					int AptekaClientCode;
					string AptekaClientDirectory;
					string OutFileNameTemplate;
					string OutFileName;

					cmdInsert.Transaction = args.DataAdapter.SelectCommand.Transaction;

					DataSet ds = MySqlHelper.ExecuteDataset(cWork, @"
SELECT
  cd.firmcode as ClientCode,
  IncludeRegulation.IncludeType,
  i.FirmClientCode
FROM
  (usersettings.clientsdata as cd,
   usersettings.intersection i,
   usersettings.pricesdata pd)
   LEFT JOIN usersettings.includeregulation
        ON includeclientcode= cd.firmcode
WHERE
  i.clientCode = cd.FirmCode
  and i.PriceCode = pd.PriceCode
  and pd.FirmCode = ?FirmCode
  and if(IncludeRegulation.PrimaryClientCode is null, 1, IncludeRegulation.IncludeType <>2)
  and i.FirmClientCode = ?FirmClientCode
group by cd.firmcode
", new MySqlParameter("?FirmCode", drCurrent["FirmCode"]), new MySqlParameter("?FirmClientCode", FirmClientCode));

					foreach (DataRow drApteka in ds.Tables[0].Rows)
					{
						AptekaClientCode = Convert.ToInt32(drApteka["ClientCode"]);

						AptekaClientDirectory = NormalizeDir(Settings.Default.FTPOptBox) + Path.DirectorySeparatorChar + AptekaClientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + "Waybills";
						OutFileNameTemplate = AptekaClientDirectory + Path.DirectorySeparatorChar;
						OutFileName = String.Empty;

						if (!Directory.Exists(AptekaClientDirectory))
							Directory.CreateDirectory(AptekaClientDirectory);

						cmdInsert.Parameters["?ClientCode"].Value = AptekaClientCode;

						OutFileName = OutFileNameTemplate + cmdInsert.ExecuteScalar().ToString() + "_"
							+ drCurrent["ShortName"].ToString()
							+ Path.GetExtension(FileName);
						OutFileName = NormalizeFileName(OutFileName);

						if (File.Exists(OutFileName))
							try
							{
								File.Delete(OutFileName);
							}
							catch { }

						File.Copy(FileName, OutFileName);
					}

					File.Delete(FileName);

					return null;
				},
				null,
				cWork,
				true,
				null,
				false,
				delegate(ExecuteArgs args, MySqlException ex)
				{
					Ping();
				});
		}

		private void WriteLog(int logFirmCode, int logClientCode, string logFileName, string logAddition)
		{
			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), delegate(ExecuteArgs args)
			{
				MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_receive_logs (FirmCode, ClientCode, FileName, Addition, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, ?Addition, 1)", args.DataAdapter.SelectCommand.Connection);

				cmdInsert.Parameters.Add("?FirmCode", logFirmCode);
				cmdInsert.Parameters.Add("?ClientCode", logClientCode);
				cmdInsert.Parameters.Add("?FileName", logFileName);
				cmdInsert.Parameters.Add("?Addition", logAddition);
				cmdInsert.ExecuteNonQuery();

				return null;
			}, 
				null,
				cWork,
				true,
				null,
				false,
				delegate(ExecuteArgs args, MySqlException ex)
				{
					Ping();
				});

		}



	}
}
