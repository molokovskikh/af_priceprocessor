using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Net;
using NUnit.Framework;
using Inforoom.PriceProcessor;
using System.Net.Security;
using RemotePriceProcessor;
using Inforoom.PriceProcessor.Properties;

namespace PriceProcessor.Test
{
    [TestFixture]
    public class WCFTest
    {
        private ServiceHost _serviceHost;
        private const string _strProtocol = @"net.tcp://";

        [Test]
        public void Test()
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
            binding.MaxBufferSize = 5242880;    // 5 Мб 
            _serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessor), binding,
                sbUrlService.ToString());
            _serviceHost.Open();
			_serviceHost.Close();
        }
    }
}
