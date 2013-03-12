using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.ServiceModel.Dispatcher;
using System.Threading;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor;
using System.ServiceModel;
using System.Net;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using RemotePriceProcessor;

#if (!DEBUG)
using Inforoom.Downloader;
#endif

namespace Inforoom.PriceProcessor
{
	/// <summary>
	/// Класс для отслеживание работа обработчиков источников.
	/// </summary>
	public class Monitor : AbstractHandler
	{
		private readonly List<AbstractHandler> _handlers;

		private ServiceHost _priceProcessorHost;
		private ServiceHost _waybillServiceHost;
		private const string _strProtocol = @"net.tcp://";

		private static Monitor _instance;

		public int StopWaitTimeout = 5000;

		public Monitor(params AbstractHandler[] handlers)
		{
			SleepTime = 500;
			_handlers = new List<AbstractHandler>();
			_handlers.AddRange(handlers);
			tWork.Name = "MonitorThread";
		}

		private Monitor()
		{
			_handlers = new List<AbstractHandler> {
				new FormalizeHandler(),
				new IndexerHandler(),
#if (!DEBUG)
				new LANSourceHandler(),
				new FTPSourceHandler(),
				new HTTPSourceHandler(),
				new EMAILSourceHandler(),
				new WaybillEmailSourceHandler(),
				new WaybillLanSourceHandler(),
				new WaybillFtpSourceHandler(),
				new ClearArchivedPriceSourceHandler(),
				new WaybillProtekHandler(),
				new CertificateSourceHandler(),
				new CertificateCatalogHandler(),
				new DocSourceHandler()
#endif
			};
		}

		public static Monitor GetInstance()
		{
			return _instance ?? (_instance = new Monitor());
		}

		public AbstractHandler GetHandler(Type type)
		{
			foreach (var h in _handlers) {
				if (h.GetType() == type)
					return h;
			}
			return null;
		}

		public void Start()
		{
			StartWork();
		}

		//запускаем монитор с обработчиками
		public override void StartWork()
		{
			try {
				StartServices();

				foreach (var handler in _handlers) {
					try {
						handler.StartWork();
						_logger.InfoFormat("Запущен обработчик {0}.", handler.GetType().Name);
					}
					catch (Exception exHan) {
						_logger.ErrorFormat("Ошибка при старте обработчика {0}:\r\n{1}", handler.GetType().Name, exHan);
					}
				}

				base.StartWork();
				_logger.Info("PriceProcessor запущен.");
			}
			catch (Exception ex) {
				_logger.Fatal("Ошибка при старте монитора", ex);
			}
		}

		private void StartServices()
		{
			var sbUrlService = new StringBuilder();
			sbUrlService.Append(_strProtocol)
				.Append(Dns.GetHostName()).Append(":")
				.Append(Settings.Default.WCFServicePort).Append("/")
				.Append(Settings.Default.WCFServiceName);

			_priceProcessorHost = PriceProcessorWcfHelper.StartService(typeof(IRemotePriceProcessor),
				typeof(WCFPriceProcessorService),
				sbUrlService.ToString(), Settings.Default.WCFQueueName);

			_waybillServiceHost = new ServiceHost(typeof(WaybillService));

			var binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			_waybillServiceHost.AddServiceEndpoint(typeof(IWaybillService),
				binding,
				String.Format("net.tcp://{0}:901/WaybillService", Dns.GetHostName()));
			_waybillServiceHost.Description.Behaviors.Add(new ErrorHandlerBehavior());
			_waybillServiceHost.Open();
		}

		//Остановливаем монитор
		public void Stop()
		{
			try {
				SoftStop();

				foreach (var handler in _handlers) {
					handler.SoftStop();
				}
				//сначала мы просим что бы нитки остановились и даем им время на это
				Thread.Sleep(StopWaitTimeout);
				HardStop();

				//теперь мы убиваем те нитки которые не остановились
				foreach (var handler in _handlers)
					try {
						_logger.InfoFormat("Попытка останова обработчика {0}.", handler.GetType().Name);
						handler.HardStop();
						_logger.InfoFormat("Обработчик {0} остановлен.", handler.GetType().Name);
					}
					catch (Exception exHan) {
						_logger.ErrorFormat("Ошибка при останове обработчика {0}:\r\n{1}", handler.GetType().Name, exHan);
					}
				PriceProcessorWcfHelper.StopService(_priceProcessorHost);
				PriceProcessorWcfHelper.StopService(_waybillServiceHost);
			}
			catch (Exception ex) {
				_logger.Fatal("Ошибка при останове монитора", ex);
			}
			_logger.Info("PriceProcessor остановлен.");
		}

		public override void ProcessData()
		{
			var deadHandlers = _handlers.Where(h => !h.Worked).ToArray();
			foreach (var handler in deadHandlers) {
				try {
					_logger.DebugFormat("Попытка остановки обработчика {0}", handler);
					handler.SoftStop();
					Thread.Sleep(StopWaitTimeout);

					handler.HardStop();

					_handlers.Remove(handler);
					_logger.DebugFormat("Обработчик остановлен {0}", handler);

					var type = handler.GetType();
					_logger.DebugFormat("Попытка запуска обработчика {0}", type);
					var newHandler = (AbstractHandler)Activator.CreateInstance(type);
					newHandler.StartWork();
					_handlers.Add(newHandler);
					_logger.DebugFormat("Обработчик запущен {0}", type);
				}
				catch(Exception e) {
					_logger.Error(String.Format("Ошибка при останоке обработчика {0}", handler), e);
				}
			}
		}
	}
}