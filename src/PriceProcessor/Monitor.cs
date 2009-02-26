using System;
using System.Collections.Generic;
using System.Threading;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using Inforoom.PriceProcessor.Properties;


namespace Inforoom.PriceProcessor
{
	/// <summary>
	/// Класс для отслеживание работа обработчиков источников.
	/// </summary>
	public class Monitor
	{
		private readonly List<AbstractHandler> _handlers;
		private readonly Thread tMonitor;
		private readonly log4net.ILog _logger;

		private bool Stopped;

		public Monitor()
		{
			_logger = log4net.LogManager.GetLogger(typeof(Monitor));

			ChannelServices.RegisterChannel(new HttpChannel(Settings.Default.RemotingPort), false);

			RemotingConfiguration.RegisterWellKnownServiceType(
			  typeof(RemotePricePricessorService),
			  Settings.Default.RemotingServiceName,
			  WellKnownObjectMode.Singleton);

			RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

			_handlers = new List<AbstractHandler>
			             	{
			             		new FormalizeHandler(),
#if (!DEBUG)
			             		new LANSourceHandler(),
			             		new FTPSourceHandler(),
			             		new HTTPSourceHandler(),
			             		new EMAILSourceHandler(),
			             		new WaybillSourceHandler(),
			             		new WaybillLANSourceHandler(),
			             		new ClearArchivedPriceSourceHandler()
#endif
			             	};

			tMonitor = new Thread(MonitorWork) {Name = "MonitorThread"};
		}

		//запускаем монитор с обработчиками
		public void Start()
		{
            try
            {
				foreach (var handler in _handlers)
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
                Thread.Sleep(3000);
                tMonitor.Abort();
				foreach (var handler in _handlers)
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
					foreach(var handler in _handlers)
					{
						if (!handler.Worked)
							handler.RestartWork();
					}

					Thread.Sleep(500);
				}
				catch(Exception e)
				{
					_logger.Error("Ошибка в нитке", e);
				}
		}
	}
}
