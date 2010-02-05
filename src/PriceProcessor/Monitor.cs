using System;
using System.Collections.Generic;
using System.Threading;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Properties;
using System.ServiceModel;
using System.Net;
using System.Text;
using log4net;
using RemotePriceProcessor;
#if (!DEBUG)
using Inforoom.PriceProcessor.Downloader;
using Inforoom.Downloader;
using System.Collections;
using System.Runtime.Serialization.Formatters;
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

		private ServiceHost _serviceHost;
		private const string _strProtocol = @"net.tcp://";

		public Monitor()
		{
			_handlers = new List<AbstractHandler> {
				new FormalizeHandler(),
#if (!DEBUG)
				new LANSourceHandler(),
				new FTPSourceHandler(),
				new HTTPSourceHandler(),
				new EMAILSourceHandler(),
				new WaybillSourceHandler(),
				new WaybillLANSourceHandler(),
				new ClearArchivedPriceSourceHandler(),
				new RostaHandler()
#endif
			};

			_monitor = new Thread(MonitorWork) {Name = "MonitorThread"};
		}

		//��������� ������� � �������������
		public void Start()
		{
            try
            {
				var sbUrlService = new StringBuilder();
				_serviceHost = new ServiceHost(typeof(WCFPriceProcessorService));
				sbUrlService.Append(_strProtocol)
					.Append(Dns.GetHostName()).Append(":")
					.Append(Settings.Default.WCFServicePort).Append("/")
					.Append(Settings.Default.WCFServiceName);

            	_serviceHost = PriceProcessorWcfHelper.StartService(typeof (IRemotePriceProcessor),
            	                                                    typeof (WCFPriceProcessorService),
            	                                                    sbUrlService.ToString());
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
				PriceProcessorWcfHelper.StopService(_serviceHost);
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
					_logger.Error("������ � �����", e);
				}
		}
	}
}
