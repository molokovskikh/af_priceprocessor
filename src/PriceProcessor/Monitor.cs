using System;
using System.Collections.Generic;
using System.Threading;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using Inforoom.PriceProcessor.Properties;
using System.Collections;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.ServiceModel;
using System.Net;
using System.Text;
using System.Net.Security;
using RemotePriceProcessor;
using System.Text.RegularExpressions;


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

		private ServiceHost _serviceHost;
		private const string _strProtocol = @"net.tcp://";

		public Monitor()
		{
			_logger = log4net.LogManager.GetLogger(typeof(Monitor));

			try
			{
				ChannelServices.RegisterChannel(new HttpChannel(Settings.Default.RemotingPort), false);

				//Создаем провайдер, чтобы не было нарушения безопасности
				var provider = new BinaryServerFormatterSinkProvider();
				provider.TypeFilterLevel = TypeFilterLevel.Full;
				//Устанавливаем свойства провайдера
				IDictionary props = new Hashtable();
				props["port"] = Settings.Default.RemotingPort + 1;
				//Без установки этого свойства тоже работает, но в примерах оно тоже установлено
				props["typeFilterLevel"] = "Full";

				var tcpChannel = new TcpChannel(props, null, provider);
				ChannelServices.RegisterChannel(tcpChannel, false);

				RemotingConfiguration.RegisterWellKnownServiceType(
				  typeof(RemotePriceProcessorService),
				  Settings.Default.RemotingServiceName,
				  WellKnownObjectMode.Singleton);

				RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
			}
			catch (Exception exception)
			{
				_logger.Fatal("Ошибка при старте Remoting", exception);
			}

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
				StringBuilder sbUrlService = new StringBuilder();
				_serviceHost = new ServiceHost(typeof(WCFPriceProcessorService));
				sbUrlService.Append(_strProtocol)
					.Append(Dns.GetHostName()).Append(":")
					.Append(Settings.Default.WCFServicePort).Append("/")
					.Append(Settings.Default.WCFServiceName);
				NetTcpBinding binding = new NetTcpBinding();
				binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
				binding.Security.Mode = SecurityMode.None;
				// Ипользуется потоковая передача данных в обе стороны 
				binding.TransferMode = TransferMode.Streamed;
				// Максимальный размер принятых данных
				binding.MaxReceivedMessageSize = Int32.MaxValue;
				// Максимальный размер одного пакета
				binding.MaxBufferSize = 524288;    // 0.5 Мб 
				_serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessor), binding,
					sbUrlService.ToString());
				_serviceHost.Open();

                foreach (var handler in _handlers)
                    try
                    {
                        handler.StartWork();
                        _logger.InfoFormat("Запущен обработчик {0}.", handler.GetType().Name);
                    }
                    catch (Exception exHan)
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
				try
				{
					_serviceHost.Close();
				}
				catch (Exception e)
				{
					_logger.Error("Ошибка остановки WCF службы", e);
				}
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
