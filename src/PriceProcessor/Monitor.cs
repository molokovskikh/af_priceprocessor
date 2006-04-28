using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Inforoom.Formalizer;

namespace Inforoom.Downloader
{
	/// <summary>
	/// Класс для отслеживание работа обработчиков источников.
	/// </summary>
	public class Monitor
	{
		private List<BaseSourceHandler> alHandlers = null;
		private bool Stopped = false;

		private Thread tMonitor;


		public Monitor()
		{
			alHandlers = new List<BaseSourceHandler>();
            alHandlers.Add(new LANSourceHandler("LAN"));
            alHandlers.Add(new FTPSourceHandler("FTP"));
            alHandlers.Add(new HTTPSourceHandler("HTTP"));
            alHandlers.Add(new EMAILSourceHandler("EMAIL"));
            tMonitor = new Thread(new ThreadStart(MonitorWork));
		}

		//запускаем монитор с обработчиками
		public void Start()
		{
            try
            {
                FormLog.Log("Monitor", "Downloader started.");
                foreach (BaseSourceHandler bsh in alHandlers)
                    try
                    {
                        bsh.StartWork();
                    }
                    catch(Exception exHan)
                    {
                        FormLog.Log("Monitor.Start." + bsh.GetType().Name, "Ошибка при старте обработчика : {0}", exHan);
                    }
                tMonitor.Start();
            }
            catch (Exception ex)
            {
                FormLog.Log("Monitor.Start", "Ошибка при старте монитора : {0}", ex);
            }
		}

		//Остановливаем монитор
		public void Stop()
		{
            try
            {
                Stopped = true;
                System.Threading.Thread.Sleep(3000);
                tMonitor.Abort();
                foreach (BaseSourceHandler bs in alHandlers)
                    try
                    {
                        bs.StopWork();
                    }
                    catch (Exception exHan)
                    {
                        FormLog.Log("Monitor.Stop." + bs.GetType().Name, "Ошибка при останове обработчика : {0}", exHan);
                    }
            }
            catch (Exception ex)
            {
                FormLog.Log("Monitor.Stop", "Ошибка при останове монитора : {0}", ex);
            }
            FormLog.Log("Monitor", "Downloader stoppped.");
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
