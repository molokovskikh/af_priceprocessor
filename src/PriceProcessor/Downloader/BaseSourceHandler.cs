using System;
using System.Threading;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using Inforoom.Downloader.Properties;

namespace Inforoom.Downloader
{
	/// <summary>
	/// Summary description for BaseSourceHandle.
	/// </summary>
	public abstract class BaseSourceHandler
	{
		//������ �� ������� �����
		protected Thread tWork;
		//����� "������" �����
		protected int SleepTime;
		//����� ���������� "�������" �����������
		protected DateTime lastPing;

		//���������� ��� �����������
		protected MySqlConnection cLog;

		//���������� ��� ������
		protected MySqlConnection cWork;

		public BaseSourceHandler()
		{
			Ping();
			tWork = new Thread(new ThreadStart(ThreadWork));
            SleepTime = Settings.Default.RequestInterval;
			CreateLogConnection();
		}

		//�������� ��?
		public bool Worked
		{
			get
			{
				return DateTime.Now.Subtract(lastPing).TotalMinutes < Settings.Default.Timeout;
			}
		}

		//������ �����������
		public void StartWork()
		{
			tWork.Start();
		}

		public void StopWork()
		{
			tWork.Abort();
			if (cLog.State == System.Data.ConnectionState.Open)
				try{ cLog.Close(); } catch{}
		}

		protected void Ping()
		{
			lastPing = DateTime.Now;
		}

		//���������� �����������
		public void RestartWork()
		{
			tWork.Abort();
			Thread.Sleep(500);
			tWork = new Thread(new ThreadStart(ThreadWork));
			tWork.Start();
			FormLog.Log( this.GetType().Name, "������������� �����");
		}

		//�����, � ������� �������������� ������ ����������� ���������
		protected void ThreadWork()
		{
			while (true)
			{
				try
				{
					ProcessData();
				}
				catch(Exception ex)
				{
					FormLog.Log( this.GetType().Name, "������ � ����� : {0}", ex);
				}
				Sleeping();				
			}
		}

		protected void Sleeping()
		{
			Thread.Sleep(SleepTime * 1000);
		}

		//����� ��� ��������� ������ ��� ������� ��������� - ����
		protected abstract void ProcessData();


		protected void CreateLogConnection()
		{
			cLog = new MySqlConnection(
				String.Format("server={0};username={1}; password={2}; database={3}; pooling=false",
					Settings.Default.DBServerName,
                    Settings.Default.DBUserName,
                    Settings.Default.DBPass,
                    Settings.Default.DatabaseName)
			);
			try
			{
				cLog.Open();
			}
			catch(Exception ex)
			{
				FormLog.Log( this.GetType().Name + ".CreateLogConnection", "{0}", ex);
			}
		}
	}
}
