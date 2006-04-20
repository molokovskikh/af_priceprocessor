using System;
using System.Collections;
using System.Threading;
using Inforoom.Formalizer;

namespace Inforoom.Downloader
{
	/// <summary>
	/// Класс для отслеживание работа обработчиков источников.
	/// </summary>
	public class Monitor
	{
		private ArrayList alHandlers = null;
		private bool Stopped = false;

		private Thread tMonitor;


		public Monitor()
		{
			alHandlers = new ArrayList();
			tMonitor = new Thread(new ThreadStart(MonitorWork));
		}

		//запускаем монитор с обработчиками
		public void Start()
		{
			tMonitor.Start();			
		}

		//Остановливаем монитор
		public void Stop()
		{
			Stopped = true;
			System.Threading.Thread.Sleep(1500);
			tMonitor.Abort();
			foreach(BaseSourceHandler bs in alHandlers)
			{
				bs.StopWork();
			}
		}

		private void MonitorWork()
		{
			while (!Stopped)
				try
				{
					foreach(BaseSourceHandler bs in alHandlers)
					{
						if (!bs.Worked)
							bs.RestartWork();
					}

					System.Threading.Thread.Sleep(500);
				}
				catch(Exception e)
				{
                    FormLog.Log("Monitor", "Ошибка в нитке : {0}", e);
				}
		}
	}
}
