using System;
using System.Net;
using System.ServiceModel;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using RemotePriceProcessor;

namespace PriceProcessor.Test.Services
{
	[TestFixture]
	public class ErrorHandlerFixture
	{
		[ServiceContract]
		public interface ITest
		{
			[OperationContract]
			void Test();
		}

		public class TestService : ITest
		{
			public void Test()
			{
				throw new Exception();
			}
		}

		[Test]
		public void Handle_error()
		{
			var host = new ServiceHost(typeof (TestService));

			var binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			var url = String.Format("net.tcp://{0}:901/Test", Dns.GetHostName());
			host.AddServiceEndpoint(typeof (ITest),
				binding,
				url);

			host.Description.Behaviors.Add(new ErrorHandlerBehavior());
			host.Open();

			var factory = new ChannelFactory<ITest>(binding, url);
			var channel = factory.CreateChannel();
			try
			{
				channel.Test();
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Is.EqualTo("Произошла ошибка. Попробуйте повторить операцию позднее."));
			}
		}
	}
}