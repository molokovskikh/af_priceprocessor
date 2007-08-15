using System;
using System.IO;
using System.Threading;
using System.Net.Mail;
using MySql.Data.MySqlClient;
using Inforoom.Logging;


namespace Inforoom.Formalizer
{

	public class FileHashItem
	{
		public string ErrorMessage = String.Empty;
		public int ErrorCount = 0;
	}
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class PriceProcessThread
	{
		//������������� ����
		private static int GlobalPPTID = -1;
		private int PPTID;
		//��� ��������������� �����
		private string fileName;
		//��������� ������� ��� �����
		private string TempPath;
		//��� �����, ������� ������� �������� ���������
		private string TempFileName;

		//���������� � �����
		private MySqlConnection myconn;
		private MySqlCommand mcLog = null;

//		private int formID;

		//�������, ����������� � ����
		private ThreadStart ts;

		//���������� �����
		private Thread t;

		//����� ������ ������������
		private DateTime tmFormalize;
		//����� ������������ � ��������
		private System.Int64 formSecs;

		public bool FormalizeEnd = false;

		//��������� ������������: ������, �����
		private bool formalizeOK = false;

		//���������� ��������� �� ������
		private string prevErrorMessage = String.Empty;
		//������� ��������� �� ������
		private string currentErrorMessage = String.Empty;
 

		private BasePriceParser WorkPrice = null;

		public PriceProcessThread(string FileName, string PrevErrorMessage)
		{
			this.tmFormalize = DateTime.UtcNow;
			this.fileName = FileName;
			this.PPTID = ++GlobalPPTID;
			this.prevErrorMessage = PrevErrorMessage;
			ts = new ThreadStart(ThreadWork);
			t = new Thread(ts);
			t.Name = String.Format("PPT{0}", PPTID);
			t.Start();

		}

		public string FileName
		{
			get
			{
				return fileName;
			}
		}

		public bool FormalizeOK
		{
			get
			{
				return formalizeOK;
			}
		}
		//�������� ����� ������ ������������
		public DateTime StartDate
		{
			get
			{
				return tmFormalize;
			}
		}

		public Thread WorkThread
		{
			get
			{
				return t;
			}
		}

		public int TID
		{
			get
			{
				return PPTID;
			}
		}

		public string CurrentErrorMessage
		{
			get
			{
				return currentErrorMessage;
			}
		}

		/// <summary>
		/// ��������� �����������
		/// </summary>
		/// <param name="Message"></param>
		private void InternalLog(string Message)
		{
			SimpleLog.Log( String.Format("TID={0}", PPTID), Message);
		}

		private void InternalLog(string format, params object[] args)
		{
			InternalLog(String.Format(format, args));
		}

		private MySqlConnection getConnection()
		{
			return new MySqlConnection(
				String.Format("server={0};username={1}; password={2}; database={3}; pooling=true; allow zero datetime=true;",
					FormalizeSettings.ServerName, 
					FormalizeSettings.UserName, 
					FormalizeSettings.Pass, 
					FormalizeSettings.DatabaseName
				)
			);
		}

		private void SuccesLog(BasePriceParser p)
		{
			string messageBody = "", messageSubject = "";
			//������������ ���������� ������ � 
			if (null == p)
			{
				SuccesGetBody("����� ������ ������������", ref messageSubject, ref messageBody, -1, -1, null);
			}
			else
			{
				SuccesGetBody("����� ������ ������������", ref messageSubject, ref messageBody, p.priceCode, p.clientCode, String.Format("{0} ({1})", p.clientShortName, p.priceName) );
			}


			if (null != mcLog)
			{
				try
				{
					try
					{
						if (myconn.State == System.Data.ConnectionState.Open)
							myconn.Close();
					}
					catch{}

					myconn.Open();
					try
					{
						mcLog.CommandText = String.Format("INSERT INTO {0} (LogTime, AppCode, PriceCode, Form, Unform, Zero, Forb, ResultId, TotalSecs) VALUES (NOW(), ?AppCode, ?PriceCode, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs );", FormalizeSettings.tbFormLogs);
						mcLog.Parameters.Clear();
						mcLog.Parameters.AddWithValue("?AppCode", FormalizeSettings.AppCode);
						mcLog.Parameters.AddWithValue("?PriceCode", p.priceCode);
						mcLog.Parameters.AddWithValue("?Form", p.formCount);
						mcLog.Parameters.AddWithValue("?Unform", p.unformCount);
						mcLog.Parameters.AddWithValue("?Zero", p.zeroCount);
						mcLog.Parameters.AddWithValue("?Forb", p.forbCount);
						mcLog.Parameters.AddWithValue("?ResultId", (p.maxLockCount <= FormalizeSettings.MinRepeatTranCount) ? 2 : 3);
						mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
						mcLog.ExecuteNonQuery();
					}
					finally
					{
						myconn.Close();
					}
				}
				catch(Exception e)
				{
					InternalLog("SuccesLog : {0}", e);
				}
			}

			if (prevErrorMessage != String.Empty)
				InternalMailSend(messageSubject, messageBody);
		}

		private void InternalMailSendBy(string From, string To, string mSubject, string mBody)
		{
			try
			{
				MailMessage Message = new MailMessage(From, To, mSubject, mBody);
				Message.BodyEncoding = System.Text.Encoding.UTF8;
				SmtpClient Client = new SmtpClient("box.analit.net");
				Client.Send(Message);
			}
			catch(Exception e)
			{
				InternalLog("ErrorLog.InternalMailSendBy : {0}", e);
			}
		}

		private void InternalMailSend(string mSubject, string mBody)
		{
			InternalMailSendBy(FormalizeSettings.FromEmail, FormalizeSettings.RepEmail, mSubject, mBody);
		}

		private void SuccesGetBody(string mSubjPref, ref string mSubj, ref string mBody, long priceCode, long clientCode, string priceName)
		{
			if (-1 == priceCode)
			{
				mSubj = mSubjPref;
				mBody = String.Format(
					@"����         : {0}
���� ������� : {1}", 
					Path.GetFileName(fileName),
					DateTime.Now);
			}
			else
			{
				mSubj = String.Format("{0} {1}", mSubjPref, priceCode);
				mBody = String.Format(
					@"��� �����       : {0}
��� ������      : {1}
�������� ������ : {2}
���� �������    : {3}", 
					clientCode,
					priceCode,
					priceName,
					DateTime.Now);
			}
		}

		private void GetBody(string mSubjPref, ref string Add, ref string mSubj, ref string mBody, long priceCode, long clientCode, string priceName)
		{
			if (-1 == priceCode)
			{
				mSubj = mSubjPref;
				mBody = String.Format(
@"����         : {0}
���� ������� : {1}
������       : {2}", 
					Path.GetFileName(fileName),
					DateTime.Now,
					Add);
				Add = mBody;
			}
			else
			{
				mSubj = String.Format("{0} {1}", mSubjPref, priceCode);
				mBody = String.Format(
@"��� �����       : {0}
��� ������      : {1}
�������� ������ : {2}
���� �������    : {3}
������          : {4}", 
					clientCode,
					priceCode,
					priceName,
					DateTime.Now,
					Add);
			}
		}

		private void ErrodLog(BasePriceParser p, Exception ex)
		{
			string messageBody = "", messageSubject = "", Addition;
			if (ex is FormalizeException)
				currentErrorMessage = ex.Message;
			else
				currentErrorMessage = ex.ToString();
			Addition = currentErrorMessage;
			InternalLog("Addition : {0}", Addition);

			//���� ���������� ��������� �� ���������� �� ��������, �� �� �������� ���
			if (prevErrorMessage != currentErrorMessage)
			{
				//������������ ���������� ������ � 
				if (null != p)
				{
					GetBody("������ ������������", ref Addition, ref messageSubject, ref messageBody, p.priceCode, p.clientCode, String.Format("{0} ({1})", p.clientShortName, p.priceName) );
				}
				else
					if (ex is FormalizeException)
				{
					GetBody("������ ������������", ref Addition, ref messageSubject, ref messageBody, ((FormalizeException)ex).priceCode, ((FormalizeException)ex).clientCode, ((FormalizeException)ex).FullName );
				}
				else
				{
					GetBody("������ ������������", ref Addition, ref messageSubject, ref messageBody, -1, -1, null);
				}

				//�������� ������������ � ����
				if (null != mcLog)
				{
					try
					{
						try
						{
							if (myconn.State == System.Data.ConnectionState.Open)
								myconn.Close();
						}
						catch{}

						myconn.Open();
						try
						{

							if ((null != p) || (ex is FormalizeException))
							{
								mcLog.CommandText = String.Format("INSERT INTO {0} (LogTime, AppCode, PriceCode, Addition, ResultId, TotalSecs) VALUES (NOW(), ?AppCode, ?PriceCode, ?Addition, ?ResultId, ?TotalSecs);", FormalizeSettings.tbFormLogs);
								mcLog.Parameters.Clear();
								mcLog.Parameters.AddWithValue("?PriceCode", (null != p) ? p.priceCode : ((FormalizeException)ex).priceCode);
							}
							else
							{
								mcLog.CommandText = String.Format("INSERT INTO {0} (LogTime, AppCode, Addition, ResultId, TotalSecs) VALUES (NOW(), ?AppCode, ?Addition, ?ResultId, ?TotalSecs);", FormalizeSettings.tbFormLogs);
								mcLog.Parameters.Clear();
							}
							mcLog.Parameters.AddWithValue("?AppCode", FormalizeSettings.AppCode);
							mcLog.Parameters.AddWithValue("?Addition", Addition);
							mcLog.Parameters.AddWithValue("?ResultId", 5);
							mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
							mcLog.ExecuteNonQuery();
						}
						finally
						{
							myconn.Close();
						}
					}
					catch(Exception e)
					{
						InternalLog("ErrorLog : {0}", e);
					}
				}

				//�������� ��������� ������ �� �����
				InternalMailSend(messageSubject, messageBody);
			}

		}

		private void WarningLog(FormalizeException e, string Addition)
		{
			string messageBody = "", messageSubject = "";
			currentErrorMessage = Addition;
			InternalLog("Addition : {0}", Addition);

			if (prevErrorMessage != currentErrorMessage)
			{
				//������������ ���������� ������ � 
				GetBody("��������������", ref Addition, ref messageSubject, ref messageBody, e.priceCode, e.clientCode, e.FullName);

				if (e is RollbackFormalizeException)
				{
					RollbackFormalizeException re = (RollbackFormalizeException)e;
					messageBody = String.Format(
						@"{0}

�������������� : {1}
���������������� : {2}
������� : {3}
����������� : {4}", 
						messageBody, 
						re.FormCount,
						re.UnformCount,
						re.ZeroCount,
						re.ForbCount);
				}

				//�������� ������������ � ����
				if (null != mcLog)
				{
					try
					{
						try
						{
							if (myconn.State == System.Data.ConnectionState.Open)
								myconn.Close();
						}
						catch{}

						myconn.Open();
						try
						{
							if (-1 == e.priceCode)
							{
								mcLog.CommandText = String.Format("INSERT INTO {0} (LogTime, AppCode, Addition, ResultId, TotalSecs) VALUES (NOW(), ?AppCode, ?Addition, ?ResultId, ?TotalSecs);", FormalizeSettings.tbFormLogs);
								mcLog.Parameters.Clear();
								mcLog.Parameters.AddWithValue("?AppCode", FormalizeSettings.AppCode);
							}
							else
							{
								mcLog.CommandText = String.Format("INSERT INTO {0} (LogTime, AppCode, PriceCode, Addition,Form, Unform, Zero, Forb, ResultId, TotalSecs) VALUES (NOW(), ?AppCode, ?PriceCode, ?Addition, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs);", FormalizeSettings.tbFormLogs);
								mcLog.Parameters.Clear();
								mcLog.Parameters.AddWithValue("?AppCode", FormalizeSettings.AppCode);
								mcLog.Parameters.AddWithValue("?PriceCode", e.priceCode);
								if (e is RollbackFormalizeException)
								{
									mcLog.Parameters.AddWithValue("?Form", ((RollbackFormalizeException)e).FormCount);
									mcLog.Parameters.AddWithValue("?Unform", ((RollbackFormalizeException)e).UnformCount);
									mcLog.Parameters.AddWithValue("?Zero", ((RollbackFormalizeException)e).ZeroCount);
									mcLog.Parameters.AddWithValue("?Forb", ((RollbackFormalizeException)e).ForbCount);
								}
								else
								{
									mcLog.Parameters.AddWithValue("?Form", DBNull.Value);
									mcLog.Parameters.AddWithValue("?Unform", DBNull.Value);
									mcLog.Parameters.AddWithValue("?Zero", DBNull.Value);
									mcLog.Parameters.AddWithValue("?Forb", DBNull.Value);
								}
							}
							mcLog.Parameters.AddWithValue("?Addition", Addition);
							mcLog.Parameters.AddWithValue("?ResultId", 5);
							mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
							mcLog.ExecuteNonQuery();
						}
						finally
						{
							myconn.Close();
						}
					}
					catch(Exception ex)
					{
						InternalLog("WarningLog : {0}", ex);
					}
				}

				//�������� ��������� ������ �� �����
				InternalMailSend(messageSubject, messageBody);
			}
		}

		/// <summary>
		/// ��������� ������������
		/// </summary>
		public void ThreadWork()
		{
			string workStr = String.Empty;
			//TempPath = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ) + "\\" + PPTID + "\\";
			TempPath = Path.GetTempPath() + Path.GetFileNameWithoutExtension(fileName) + "\\";
			TempFileName = TempPath + Path.GetFileName(fileName);
			InternalLog("�������� ����� �� ��������� ����� : {0}", fileName);
			try
			{
				myconn = getConnection();
				try
				{
					//������� ������� ��� �����������
					mcLog = new MySqlCommand();
					mcLog.Connection = myconn;

					//������� ���������� ��� ���������� ����� � �������� ���� ����
					Directory.CreateDirectory(TempPath);

					try
					{
						try
						{
							WorkPrice = PricesValidator.Validate(myconn, fileName, TempFileName);
						}
						finally
						{
							if (myconn.State == System.Data.ConnectionState.Open)
								try
								{
									myconn.Close();
								}
								catch
								{}
						}

						WorkPrice.Formalize();

						formalizeOK = true;
					}
					finally
					{
						TimeSpan tsFormalize = DateTime.UtcNow.Subtract(tmFormalize);
						formSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
						workStr = tsFormalize.ToString();
					}

					try
					{
						if (formalizeOK)
						{
							//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
							File.Copy(TempFileName, FormalizeSettings.BasePath + Path.GetFileName(fileName), true);
							DateTime ft = DateTime.UtcNow;
							File.SetCreationTimeUtc(FormalizeSettings.BasePath + Path.GetFileName(fileName), ft);
							File.SetLastWriteTimeUtc(FormalizeSettings.BasePath + Path.GetFileName(fileName), ft);
							File.SetLastAccessTimeUtc(FormalizeSettings.BasePath + Path.GetFileName(fileName), ft);
						}
					}
					catch(Exception e)
					{
						throw new FormalizeException(
							String.Format(FormalizeSettings.FileCopyError, TempFileName, FormalizeSettings.BasePath, e), 
							WorkPrice.clientCode, 
							WorkPrice.priceCode, 
							WorkPrice.clientShortName, 
							WorkPrice.priceName);
					}

					SuccesLog(WorkPrice);
				}
				catch(WarningFormalizeException e)
				{
					//���� �������� ������������ ������, �� �������, ��� ������������ ����������� ������
					try
					{
						//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
						if (File.Exists(TempFileName))
							File.Copy(TempFileName, FormalizeSettings.BasePath + Path.GetFileName(fileName), true);
						else
							//�������� ������������ ���� � ������ ������������ �����
							File.Copy(fileName, FormalizeSettings.BasePath + Path.GetFileName(fileName), true);
						DateTime ft = DateTime.UtcNow;
						File.SetCreationTimeUtc(FormalizeSettings.BasePath + Path.GetFileName(fileName), ft);
						File.SetLastWriteTimeUtc(FormalizeSettings.BasePath + Path.GetFileName(fileName), ft);
						File.SetLastAccessTimeUtc(FormalizeSettings.BasePath + Path.GetFileName(fileName), ft);
						WarningLog(e, e.Message);
						formalizeOK = true;
					}
					catch(Exception ex)
					{
						formalizeOK = false;
						ErrodLog(WorkPrice, 
							new FormalizeException(
								String.Format(FormalizeSettings.FileCopyError, TempFileName, FormalizeSettings.BasePath, ex), 
								e.clientCode, 
								e.priceCode, 
								e.clientName, 
								e.priceName));
					}
				}
				catch(FormalizeException e)
				{
					formalizeOK = false;
					ErrodLog(WorkPrice, e);
				}
				catch(Exception e)
				{
					formalizeOK = false;
					if ( !(e is System.Threading.ThreadAbortException) )
						ErrodLog(WorkPrice, e);
					else
						ErrodLog(WorkPrice, new Exception(FormalizeSettings.ThreadAbortError));
				}
				finally
				{
					try
					{
						myconn.Close();
						myconn.Dispose();
					}
					catch
					{}
				}

			}
			catch(Exception e)
			{
				InternalLog( e.ToString() );
				InternalMailSendBy(FormalizeSettings.FromEmail, "morozov@analit.net", "ThreadWork Error", e.ToString());
			}
			finally
			{
				GC.Collect();
				try
				{
					File.Delete(TempFileName);
				}
				catch(Exception e)
				{
					InternalLog("������ ��� �������� {0}", e);
				}
				InternalLog( "����� ��������� ������ � ������� {0}: {1}.", fileName, workStr);
				FormalizeEnd = true;
			}
		}
	}
}
