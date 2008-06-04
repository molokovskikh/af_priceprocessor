using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Inforoom.Logging;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using Inforoom.PriceProcessor.Properties;
using RemotePricePricessor;


namespace Inforoom.PriceProcessor
{
	/// <summary>
	/// Класс для отслеживание работа обработчиков источников.
	/// </summary>
	public class Monitor
	{
		private List<AbstractHandler> alHandlers = null;
		private bool Stopped = false;

		private Thread tMonitor;


		public Monitor()
		{
			ChannelServices.RegisterChannel(new HttpChannel(Settings.Default.RemotingPort), false);

			RemotingConfiguration.RegisterWellKnownServiceType(
			  typeof(RemotePricePricessorService),
			  Settings.Default.RemotingServiceName,
			  WellKnownObjectMode.Singleton);

			RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

			alHandlers = new List<AbstractHandler>();

			alHandlers.Add(new FormalizeHandler());

			alHandlers.Add(new LANSourceHandler());
			alHandlers.Add(new FTPSourceHandler());
			alHandlers.Add(new HTTPSourceHandler());
			alHandlers.Add(new EMAILSourceHandler());
			alHandlers.Add(new WaybillSourceHandler());
			alHandlers.Add(new WaybillLANSourceHandler());
			alHandlers.Add(new ClearArchivedPriceSourceHandler());

			tMonitor = new Thread(new ThreadStart(MonitorWork));
		}

		//запускаем монитор с обработчиками
		public void Start()
		{
            try
            {
				foreach (AbstractHandler handler in alHandlers)
                    try
                    {
                        handler.StartWork();
                    }
                    catch(Exception exHan)
                    {
                        SimpleLog.Log("Monitor.Start." + handler.GetType().Name, "Ошибка при старте обработчика : {0}", exHan);
                    }
                tMonitor.Start();
				SimpleLog.Log("Monitor", "PriceProcessor запущен.");
			}
            catch (Exception ex)
            {
                SimpleLog.Log("Monitor.Start", "Ошибка при старте монитора : {0}", ex);
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
				foreach (AbstractHandler handler in alHandlers)
                    try
                    {
                        handler.StopWork();
                    }
                    catch (Exception exHan)
                    {
                        SimpleLog.Log("Monitor.Stop." + handler.GetType().Name, "Ошибка при останове обработчика : {0}", exHan);
                    }
            }
            catch (Exception ex)
            {
                SimpleLog.Log("Monitor.Stop", "Ошибка при останове монитора : {0}", ex);
            }
			SimpleLog.Log("Monitor", "PriceProcessor остановлен.");
        }

		private void MonitorWork()
		{
			while (!Stopped)
				try
				{
					foreach(AbstractHandler handler in alHandlers)
					{
						if (!handler.Worked)
							handler.RestartWork();
					}

					System.Threading.Thread.Sleep(500);
				}
				catch(Exception e)
				{
                    SimpleLog.Log("Monitor", "Ошибка в нитке : {0}", e);
				}
		}
	}
}
