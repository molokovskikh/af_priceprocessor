using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Messaging;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.Net.Security;
using System.IO;
using log4net;
using Message = System.ServiceModel.Channels.Message;

namespace RemotePriceProcessor
{
	public class ErrorHandlerBehavior : IServiceBehavior
	{
		public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
		}

		public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			foreach (var dispatcher in serviceHostBase.ChannelDispatchers.OfType<ChannelDispatcher>())
				dispatcher.ErrorHandlers.Add(new ErrorHandler());
		}
	}

	public class ErrorHandler : IErrorHandler
	{
		private ILog log = LogManager.GetLogger(typeof(ErrorHandler));

		public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
		{
			if (fault == null) {
				var faultException = new FaultException("Произошла ошибка. Попробуйте повторить операцию позднее.");

				var message = Message.CreateMessage(version, faultException.CreateMessageFault(), faultException.Action);
				fault = message;
			}
		}

		public bool HandleError(Exception error)
		{
			if (error is FaultException)
				return true;
			log.Error("Ошибка при обращении к сервису", error);
			return true;
		}
	}

	/// <summary>
	/// Класс для вызовов методов Wcf сервиса RemotePriceProcessor
	/// </summary>
	public class PriceProcessorWcfHelper
	{
		protected ChannelFactory<IRemotePriceProcessor> _channelFactory;
		protected ChannelFactory<IRemotePriceProcessorOneWay> _msmqChannelFactory;

		private IRemotePriceProcessor _clientProxy;
		private IRemotePriceProcessorOneWay _msmqClientProxy;

		private const int MaxBufferSize = 524288;

		public PriceProcessorWcfHelper(string wcfServiceUrl) : this(wcfServiceUrl, string.Empty)
		{
		}

		public PriceProcessorWcfHelper(string wcfServiceUrl, string wcfQueueName)
		{
			var binding = CreateTcpBinding();
			_channelFactory = new ChannelFactory<IRemotePriceProcessor>(binding, wcfServiceUrl);
			_channelFactory.Endpoint.Behaviors.Add(new MessageInspectorRegistrator(new[] { new AuditInfoInspector() }));
			if (!string.IsNullOrEmpty(wcfQueueName)) {
				var msmqBinding = CreateMsmqBinding();
				_msmqChannelFactory = new ChannelFactory<IRemotePriceProcessorOneWay>(msmqBinding, wcfQueueName);
				_msmqChannelFactory.Endpoint.Behaviors.Add(new MessageInspectorRegistrator(new[] { new AuditInfoInspector() }));
			}
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
			binding.SendTimeout += new TimeSpan(0, 1, 0);
			binding.ReceiveTimeout += new TimeSpan(0, 1, 0);
			return binding;
		}

		public static NetMsmqBinding CreateMsmqBinding()
		{
			var binding = new NetMsmqBinding();
			binding.Security.Transport.MsmqProtectionLevel = ProtectionLevel.EncryptAndSign;
			binding.Security.Mode = NetMsmqSecurityMode.None;
			binding.MaxReceivedMessageSize = Int32.MaxValue;
			binding.Name = "MsmqBindingNonTransactionalNoSecurity";
			binding.ExactlyOnce = false;
			return binding;
		}

		public static ServiceHost StartService(Type serviceInterfaceType, Type serviceImplementationType, string wcfServiceUrl, string wcfQueueName)
		{
			var serviceHost = new ServiceHost(serviceImplementationType);
			var tcpBinding = CreateTcpBinding();
			var msmqBinding = CreateMsmqBinding();
			serviceHost.AddServiceEndpoint(serviceInterfaceType, tcpBinding, wcfServiceUrl);

			var queueName = GetShortQueueName(wcfQueueName);
			if (!string.IsNullOrEmpty(queueName))
				if (!MessageQueue.Exists(queueName))
					MessageQueue.Create(queueName, false);

			serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessorOneWay), msmqBinding, wcfQueueName);
			serviceHost.Description.Behaviors.Add(new ErrorHandlerBehavior());
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

		public static void StopService(ServiceHost serviceHost)
		{
			try {
				serviceHost.Close();
			}
			catch (Exception) {
			}
		}

		private void AbortClientProxy()
		{
			if (((ICommunicationObject)_clientProxy).State != CommunicationState.Closed)
				((ICommunicationObject)_clientProxy).Abort();
		}

		private void AbortMsmqClientProxy()
		{
			if (((ICommunicationObject)_msmqClientProxy).State != CommunicationState.Closed)
				((ICommunicationObject)_msmqClientProxy).Abort();
		}

		public bool ResendPrice(ulong downlogId)
		{
			LastErrorMessage = String.Empty;
			try {
				var parameter = new WcfCallParameter {
					Value = downlogId,
					LogInformation = new LogInformation {
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};
				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.ResendPrice(parameter);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally {
				AbortClientProxy();
			}
			return true;
		}

		public void RetransPriceSmart(uint priceId)
		{
			try {
				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.RetransPriceSmart(priceId);
				((ICommunicationObject)_clientProxy).Close();
			}
			finally {
				AbortClientProxy();
			}
		}

		public void RetransPriceSmartMsMq(uint priceId)
		{
			try {
				if (_msmqChannelFactory != null) {
					_msmqClientProxy = _msmqChannelFactory.CreateChannel();
					_msmqClientProxy.RetransPriceSmart(priceId);
					((ICommunicationObject)_msmqClientProxy).Close();
				}
			}
			finally {
				AbortMsmqClientProxy();
			}
		}

		public bool RetransPrice(ulong priceItemId)
		{
			return RetransPrice(priceItemId, false);
		}

		public bool RetransPrice(ulong priceItemId, bool msmqUse)
		{
			LastErrorMessage = String.Empty;
			try {
				var parameter = new WcfCallParameter() {
					Value = priceItemId,
					LogInformation = new LogInformation() {
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};
				if (!msmqUse) {
					_clientProxy = _channelFactory.CreateChannel();
					_clientProxy.RetransPrice(parameter);
					((ICommunicationObject)_clientProxy).Close();
				}
				else if (_msmqChannelFactory != null) {
					_msmqClientProxy = _msmqChannelFactory.CreateChannel();
					_msmqClientProxy.RetransPrice(parameter);
					((ICommunicationObject)_msmqClientProxy).Close();
				}
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally {
				if (!msmqUse)
					AbortClientProxy();
				else if (_msmqChannelFactory != null)
					AbortMsmqClientProxy();
			}
			return true;
		}

		public string[] ErrorFiles()
		{
			LastErrorMessage = String.Empty;
			var files = new string[0];
			try {
				_clientProxy = _channelFactory.CreateChannel();
				files = _clientProxy.ErrorFiles();
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return new string[0];
			}
			finally {
				AbortClientProxy();
			}
			return files;
		}

		public string[] InboundFiles()
		{
			LastErrorMessage = String.Empty;
			var files = new string[0];
			try {
				_clientProxy = _channelFactory.CreateChannel();
				files = _clientProxy.InboundFiles();
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return new string[0];
			}
			finally {
				AbortClientProxy();
			}
			return files;
		}

		public WcfPriceProcessItem[] GetPriceItemList()
		{
			return UniversavWcfCall(() => _clientProxy.GetPriceItemList());
		}

		public bool TopInInboundList(int hashCode)
		{
			return UniversavWcfCall(() => _clientProxy.TopInInboundList(hashCode));
		}

		public bool DeleteItemInInboundList(int hashCode)
		{
			return UniversavWcfCall(() => _clientProxy.DeleteItemInInboundList(hashCode));
		}

		public T UniversavWcfCall<T>(Func<T> action)
		{
			LastErrorMessage = String.Empty;
			var result = default(T);
			try {
				_clientProxy = _channelFactory.CreateChannel();
				result = action();
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return default(T);
			}
			finally {
				AbortClientProxy();
			}
			return result;
		}

		public Stream BaseFile(uint priceItemId)
		{
			LastErrorMessage = String.Empty;
			Stream stream = null;
			try {
				_clientProxy = _channelFactory.CreateChannel();
				stream = _clientProxy.BaseFile(priceItemId);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return null;
			}
			finally {
				AbortClientProxy();
			}
			return stream;
		}

		public HistoryFile GetFileFormHistory(ulong downlogId)
		{
			LastErrorMessage = String.Empty;
			HistoryFile historyFile = null;
			try {
				var wcfParameter = new WcfCallParameter() {
					Value = downlogId,
					LogInformation = new LogInformation() {
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};

				_clientProxy = _channelFactory.CreateChannel();
				historyFile = _clientProxy.GetFileFormHistory(wcfParameter);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return null;
			}
			finally {
				AbortClientProxy();
			}
			return historyFile;
		}

		public bool PutFileToInbound(uint priceItemId, Stream stream)
		{
			LastErrorMessage = String.Empty;
			try {
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
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally {
				AbortClientProxy();
			}
			return true;
		}

		public bool PutFileToBase(uint priceItemId, Stream stream)
		{
			LastErrorMessage = String.Empty;
			try {
				var parameter = new FilePriceInfo() {
					PriceItemId = priceItemId,
					Stream = stream,
					LogInformation = new LogInformation() {
						ComputerName = Environment.MachineName,
						UserName = Environment.UserName
					}
				};

				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.PutFileToBase(parameter);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally {
				AbortClientProxy();
			}
			return true;
		}

		public bool RetransErrorPrice(ulong priceItemId)
		{
			LastErrorMessage = String.Empty;
			try {
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
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return false;
			}
			finally {
				AbortClientProxy();
			}
			return true;
		}

		public string LastErrorMessage { get; private set; }

		public string[] FindSynonyms(uint priceItemId)
		{
			LastErrorMessage = String.Empty;
			string[] result;
			try {
				_clientProxy = _channelFactory.CreateChannel();
				result = _clientProxy.FindSynonyms(priceItemId);
				((ICommunicationObject)_clientProxy).Close();
			}
			catch (FaultException faultException) {
				LastErrorMessage = faultException.Reason.ToString();
				return new[] { "Error", LastErrorMessage };
			}
			finally {
				AbortClientProxy();
			}
			return result;
		}

		public string[] FindSynonymsResult(string taskId)
		{
			string[] result;
			try {
				_clientProxy = _channelFactory.CreateChannel();
				result = _clientProxy.FindSynonymsResult(taskId);
				((ICommunicationObject)_clientProxy).Close();
			}
			finally {
				AbortClientProxy();
			}
			return result;
		}

		public void StopFindSynonyms(string taskId)
		{
			try {
				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.StopFindSynonyms(taskId);
				((ICommunicationObject)_clientProxy).Close();
			}
			finally {
				AbortClientProxy();
			}
		}

		public void AppendToIndex(string[] synonymsIds)
		{
			try {
				_clientProxy = _channelFactory.CreateChannel();
				_clientProxy.AppendToIndex(synonymsIds);
				((ICommunicationObject)_clientProxy).Close();
			}
			finally {
				AbortClientProxy();
			}
		}
	}
}