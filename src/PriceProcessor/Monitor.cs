using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

		private readonly log4net.ILog _logger;

		public Monitor()
		{
			_logger = log4net.LogManager.GetLogger(typeof(Monitor));

			ChannelServices.RegisterChannel(new HttpChannel(Settings.Default.RemotingPort), false);

			RemotingConfiguration.RegisterWellKnownServiceType(
			  typeof(RemotePricePricessorService),
			  Settings.Default.RemotingServiceName,
			  WellKnownObjectMode.Singleton);

			RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

			alHandlers = new List<AbstractHandler>();

			alHandlers.Add(new FormalizeHandler());

#if (!DEBUG)
			alHandlers.Add(new LANSourceHandler());
			alHandlers.Add(new FTPSourceHandler());
			alHandlers.Add(new HTTPSourceHandler());
			alHandlers.Add(new EMAILSourceHandler());
			alHandlers.Add(new WaybillSourceHandler());
			alHandlers.Add(new WaybillLANSourceHandler());
			alHandlers.Add(new ClearArchivedPriceSourceHandler());
#endif

			tMonitor = new Thread(new ThreadStart(MonitorWork));
			tMonitor.Name = "MonitorThread";
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
						_logger.InfoFormat("Запущен обработчик {0}.", handler.GetType().Name);
					}
                    catch(Exception exHan)
                    {
						_logger.ErrorFormat("Ошибка при старте обработчика {0}:\r\n{1}", handler.GetType().Name, exHan);
                    }
                tMonitor.Start();
				_logger.Info("PriceProcessor запущен.");
			}
            catch (Exception ex)
            {
				_logger.Fatal("Ошибка при старте монитора", ex);
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
						_logger.InfoFormat("Попытка останова обработчика {0}.", handler.GetType().Name);
						handler.StopWork();
						_logger.InfoFormat("Обработчик {0} остановлен.", handler.GetType().Name);
					}
                    catch (Exception exHan)
                    {
						_logger.ErrorFormat("Ошибка при останове обработчика {0}:\r\n{1}", handler.GetType().Name, exHan);
                    }
            }
            catch (Exception ex)
            {
				_logger.Fatal("Ошибка при останове монитора", ex);
			}
			_logger.Info("PriceProcessor остановлен.");
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
					_logger.Error("Ошибка в нитке", e);
				}
		}
	}
}
