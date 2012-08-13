using System;
using System.Collections.Generic;
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
	/// ����� ��� ������������ ������ ������������ ����������.
	/// </summary>
	public class Monitor
	{
		private readonly List<AbstractHandler> _handlers;
		private readonly Thread _monitor;
		private readonly ILog _logger = LogManager.GetLogger(typeof(Monitor));

		private bool Stopped;

		private ServiceHost _priceProcessorHost;
		private ServiceHost _waybillServiceHost;
		private const string _strProtocol = @"net.tcp://";

		private static Monitor _instance;
		
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
				new ProtekWaybillHandler(),
				new CertificateSourceHandler(),
				new CertificateCatalogHandler(),
				new DocSourceHandler()
#endif
			};

			_monitor = new Thread(MonitorWork) {Name = "MonitorThread"};
		}

		public static Monitor GetInstance()
		{
			return _instance ?? (_instance = new Monitor());
		}

		public AbstractHandler GetHandler(Type type)
		{
			foreach (var h in _handlers)
			{
				if (h.GetType() == type)
					return h;
			}
			return null;
		}

		//��������� ������� � �������������
		public void Start()
		{
			try
			{
				StartServices();

				foreach (var handler in _handlers)
					try
					{
						handler.StartWork();
						_logger.InfoFormat("������� ���������� {0}.", handler.GetType().Name);
					}
					catch (Exception exHan)
					{
						_logger.ErrorFormat("������ ��� ������ ����������� {0}:\r\n{1}", handler.GetType().Name, exHan);
					}
				_monitor.Start();
				_logger.Info("PriceProcessor �������.");
			}
			catch (Exception ex)
			{
				_logger.Fatal("������ ��� ������ ��������", ex);
			}
		}

		private void StartServices()
		{
			var sbUrlService = new StringBuilder();
			sbUrlService.Append(_strProtocol)
				.Append(Dns.GetHostName()).Append(":")
				.Append(Settings.Default.WCFServicePort).Append("/")
				.Append(Settings.Default.WCFServiceName);

			_priceProcessorHost = PriceProcessorWcfHelper.StartService(typeof (IRemotePriceProcessor),
				typeof (WCFPriceProcessorService),
				sbUrlService.ToString(), Settings.Default.WCFQueueName);

			_waybillServiceHost = new ServiceHost(typeof (WaybillService));

			var binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			_waybillServiceHost.AddServiceEndpoint(typeof (IWaybillService),
				binding,
				String.Format("net.tcp://{0}:901/WaybillService", Dns.GetHostName()));
			_waybillServiceHost.Description.Behaviors.Add(new ErrorHandlerBehavior());
			_waybillServiceHost.Open();
		}

		//������������� �������
		public void Stop()
		{
			try
			{
				Stopped = true;
				Thread.Sleep(3000);
				_monitor.Abort();
				foreach (var handler in _handlers)
					try
					{
						_logger.InfoFormat("������� �������� ����������� {0}.", handler.GetType().Name);
						handler.StopWork();
						_logger.InfoFormat("���������� {0} ����������.", handler.GetType().Name);
					}
					catch (Exception exHan)
					{
						_logger.ErrorFormat("������ ��� �������� ����������� {0}:\r\n{1}", handler.GetType().Name, exHan);
					}
				PriceProcessorWcfHelper.StopService(_priceProcessorHost);
				PriceProcessorWcfHelper.StopService(_waybillServiceHost);
			}
			catch (Exception ex)
			{
				_logger.Fatal("������ ��� �������� ��������", ex);
			}
			_logger.Info("PriceProcessor ����������.");
		}

		private void MonitorWork()
		{
			while (!Stopped)
			{
				try
				{
					foreach (var handler in _handlers)
					{
						if (!handler.Worked)
							handler.RestartWork();
					}

					Thread.Sleep(500);
				}
				catch (Exception e)
				{
					_logger.Error("������ � �����", e);
				}
			}
		}
	}
}
