using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Net.Security;
using System.IO;

namespace RemotePriceProcessor
{
	/// <summary>
	/// Класс для вызовов методов Wcf сервиса RemotePriceProcessor
	/// </summary>
	public class PriceProcessorWcfHelper
	{
		private ChannelFactory<IRemotePriceProcessor> _channelFactory;

		private IRemotePriceProcessor _clientProxy;

		private const int MaxBufferSize = 524288;

		public PriceProcessorWcfHelper(string wcfServiceUrl)
		{
			var binding = CreateTcpBinding();
			_channelFactory = new ChannelFactory<IRemotePriceProcessor>(binding, wcfServiceUrl);
		}

		public void Dispose()
		{
			_channelFactory.Close();
		}

		private static NetTcpBinding CreateTcpBinding()
		{
			var binding = new NetTcpBinding();
			binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
			binding.Security.Mode = SecurityMode.None;
			binding.TransferMode = TransferMode.Streamed;
			binding.MaxReceivedMessageSize = Int32.MaxValue;
			binding.MaxBufferSize = MaxBufferSize;
			return binding;
		}

		public static ServiceHost StartService(Type serviceInterfaceType, Type serviceImplementationType, string wcfServiceUrl)
		{
			var serviceHost = new ServiceHost(serviceImplementationType);
			var binding = CreateTcpBinding();
			serviceHost.AddServiceEndpoint(serviceInterfaceType, binding, wcfServiceUrl);
			serviceHost.Open();
			return serviceHost;
		}

		public static void StopService(ServiceHost serviceHost)
		{
            try
            {
                serviceHost.Close();
            }
			catch (Exception)
			{}
		}

		private void AbortClientProxy()
		{
			if (((ICommunicationObject)_clientProxy).State != CommunicationState.Closed)
				((ICommunicationObject)_clientProxy).Abort();			
		}

		public bool ResendPrice(ulong downlogId)
		{
			LastErrorMessage = String.Empty;
			try
			{
				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.ResendPrice(downlogId);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally
			{
				AbortClientProxy();
			}
			return true;
		}

		public bool RetransPrice(ulong priceItemId)
		{
			LastErrorMessage = String.Empty;
			try
			{
				var parameter = new WcfCallParameter() {
					Value = priceItemId,
					LogInformation = new LogInformation() {
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};

				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.RetransPrice(parameter);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally
			{
				AbortClientProxy();
			}
			return true;
		}

		public string[] ErrorFiles()
		{
			LastErrorMessage = String.Empty;
			var files = new string[0];
			try
			{
				_clientProxy = _channelFactory.CreateChannel();
				files = _clientProxy.ErrorFiles();
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return new string[0];
			}
			finally
			{
				AbortClientProxy();
			}
			return files;
		}

		public string[] InboundFiles()
		{
			LastErrorMessage = String.Empty;
			var files = new string[0];
			try
			{
				_clientProxy = _channelFactory.CreateChannel();
				files = _clientProxy.InboundFiles();
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return new string[0];
			}
			finally
			{
				AbortClientProxy();
			}
			return files;
		}

		public string[] InboundPriceItemIds()
		{
			LastErrorMessage = String.Empty;
			var priceItemIds = new string[0];
			try
			{
				_clientProxy = _channelFactory.CreateChannel();
				priceItemIds = _clientProxy.InboundPriceItemIds();
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return new string[0];
			}
			finally
			{
				AbortClientProxy();
			}
			return priceItemIds;
		}

		public Stream BaseFile(uint priceItemId)
		{
			LastErrorMessage = String.Empty;
			Stream stream = null;
			try
			{
				_clientProxy = _channelFactory.CreateChannel();
				stream = _clientProxy.BaseFile(priceItemId);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return null;
			}
			finally
			{
				AbortClientProxy();
			}
			return stream;
		}

		public HistoryFile GetFileFormHistory(ulong downlogId)
		{
			LastErrorMessage = String.Empty;
			HistoryFile historyFile = null;
			try
			{
				var wcfParameter = new WcfCallParameter() {
                    Value = downlogId,
                    LogInformation = new LogInformation() {
                        ComputerName = Environment.MachineName,
                        UserName = Environment.UserName
                    }
                };

				_clientProxy = _channelFactory.CreateChannel();
				historyFile = _clientProxy.GetFileFormHistory(wcfParameter);
				((ICommunicationObject) _clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return null;
			}
			finally
			{
				AbortClientProxy();
			}
			return historyFile;
		}

		public bool PutFileToInbound(uint priceItemId, Stream stream)
		{
			LastErrorMessage = String.Empty;
			try
			{
				var parameter = new FilePriceInfo() {
                    PriceItemId = priceItemId,
                    Stream = stream,
                    LogInformation = new LogInformation() {
                        ComputerName = Environment.MachineName,
                        UserName = Environment.UserName
					}
                };

				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.PutFileToInbound(parameter);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally
			{
				AbortClientProxy();
			}
			return true;
		}

		public bool PutFileToBase(uint priceItemId, Stream stream)
		{
			LastErrorMessage = String.Empty;
			try
			{
				var parameter = new FilePriceInfo()
				{
					PriceItemId = priceItemId,
					Stream = stream,
					LogInformation = new LogInformation()
					{
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};

				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.PutFileToBase(parameter);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally
			{
				AbortClientProxy();
			}
			return true;
		}

		public bool RetransErrorPrice(ulong priceItemId)
		{
			LastErrorMessage = String.Empty;
			try
			{
				var wcfParameter = new WcfCallParameter() {
					Value = priceItemId,
					LogInformation = new LogInformation() {
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};

				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.RetransErrorPrice(wcfParameter);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException)
			{
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally
			{
				AbortClientProxy();
			}
			return true;
		}

		public string LastErrorMessage { get; private set; }
	}
}
