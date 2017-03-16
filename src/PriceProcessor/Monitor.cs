using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
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
using Inforoom.PriceProcessor.Wcf;
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
				new DocSourceHandler(),
				new WaybillEmailProtekHandler()
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
			_priceProcessorHost = StartService(typeof(IRemotePriceProcessor),
				typeof(WCFPriceProcessorService),
				String.Format("net.tcp://0.0.0.0:{0}/{1}",
					Settings.Default.WCFServicePort,
					Settings.Default.WCFServiceName),
				Settings.Default.WCFQueueName);

			_waybillServiceHost = StartWaybillService<WaybillService, IWaybillService>("net.tcp://0.0.0.0:901/WaybillService");
		}

		public static ServiceHost StartWaybillService<T, TContact>(string uri)
		{
			var host = new ServiceHost(typeof(T));
			var binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			host.AddServiceEndpoint(typeof(TContact), binding, uri);
			host.Description.Behaviors.Add(new ErrorHandlerBehavior());
			host.Description.Behaviors.Add(new RegisterSessionBehavior());
			host.Open();
			return host;
		}

		public static ServiceHost StartService(Type serviceInterfaceType, Type serviceImplementationType, string uri, string wcfQueueName)
		{
			var serviceHost = new ServiceHost(serviceImplementationType);
			var tcpBinding = PriceProcessorWcfHelper.CreateTcpBinding();
			var msmqBinding = PriceProcessorWcfHelper.CreateMsmqBinding();
			serviceHost.AddServiceEndpoint(serviceInterfaceType, tcpBinding, uri);

			var queueName = GetShortQueueName(wcfQueueName);
			if (!string.IsNullOrEmpty(queueName))
				if (!MessageQueue.Exists(queueName))
					MessageQueue.Create(queueName, false);

			serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessorOneWay), msmqBinding, wcfQueueName);
			serviceHost.Description.Behaviors.Add(new ErrorHandlerBehavior());
			serviceHost.Description.Behaviors.Add(new RegisterSessionBehavior());
			serviceHost.Open();
			return serviceHost;
		}

		private static string GetShortQueueName(string name)
		{
			var parts = name.Split(new[] { '/' });
			if (parts.Length > 1)
				return string.Format(@".\{1}$\{0}", parts.Last(), parts[parts.Length - 2]);
			return string.Empty;
		}

		//Останавливаем монитор
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