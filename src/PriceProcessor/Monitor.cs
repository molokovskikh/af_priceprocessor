using System;
using System.Collections.Generic;
using System.Threading;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Properties;
using System.ServiceModel;
using System.Net;
using System.Text;
using System.Net.Security;
using RemotePriceProcessor;
using Inforoom.Downloader;
using System.Collections;
using System.Runtime.Serialization.Formatters;

namespace Inforoom.PriceProcessor
{
	/// <summary>
	/// ����� ��� ������������ ������ ������������ ����������.
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

		//��������� ������� � �������������
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
				// ����������� ��������� �������� ������ � ��� ������� 
				binding.TransferMode = TransferMode.Streamed;
				// ������������ ������ �������� ������
				binding.MaxReceivedMessageSize = Int32.MaxValue;
				// ������������ ������ ������ ������
				binding.MaxBufferSize = 524288;    // 0.5 �� 
				_serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessor), binding,
					sbUrlService.ToString());
				_serviceHost.Open();

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
                tMonitor.Start();
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
				try
				{
					_serviceHost.Close();
				}
				catch (Exception e)
				{
					_logger.Error("������ ��������� WCF ������", e);
				}
                Stopped = true;
                Thread.Sleep(3000);
                tMonitor.Abort();
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
