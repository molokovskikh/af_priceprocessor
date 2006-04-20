using System;
using System.Threading;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;

namespace Inforoom.Downloader
{
	/// <summary>
	/// Summary description for BaseSourceHandle.
	/// </summary>
	public abstract class BaseSourceHandler
	{
		//Ссылка на рабочую нитку
		protected Thread tWork;
		//Время "застоя" нитки
		protected int SpeepTime;
		//Время последнего "касания" обработчика
		protected DateTime lastPing;

		//Соединение для логирования
		protected MySqlConnection cLog;

		//Соединение для работы
		protected MySqlConnection cWork;

		public BaseSourceHandler()
		{
			Ping();
			tWork = new Thread(new ThreadStart(ThreadWork));
			SpeepTime = DownloadSettings.RequestInterval;
			CreateLogConnection();
		}

		//Работает ли?
		public bool Worked
		{
			get
			{
				return DateTime.Now.Subtract(lastPing).TotalMinutes < DownloadSettings.Timeout;
			}
		}

		//Запуск обработчика
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

		//Перезапуск обработчика
		public void RestartWork()
		{
			tWork.Abort();
			Thread.Sleep(500);
			tWork = new Thread(new ThreadStart(ThreadWork));
			tWork.Start();
			FormLog.Log( this.GetType().Name, "Перезапустили нитку");
		}

		//Нитка, в которой осуществляется работа обработчика источника
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
					FormLog.Log( this.GetType().Name, "Ошибка в нитке : {0}", ex);
				}
				Sleeping();				
			}
		}

		protected void Sleeping()
		{
			Thread.Sleep(SpeepTime * 1000);
		}

		//Метод для обработки данных для каждого источника - свой
		protected abstract void ProcessData();


		protected void CreateLogConnection()
		{
			cLog = new MySqlConnection(
				String.Format("server={0};username={1}; password={2}; database={3}; pooling=false",
					DownloadSettings.ServerName,
					DownloadSettings.UserName,
					DownloadSettings.Pass,
					DownloadSettings.DatabaseName)
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
