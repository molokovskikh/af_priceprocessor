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
		private ServiceHost host;
		private ITest channel;

		[ServiceContract]
		public interface ITest
		{
			[OperationContract]
			void Test();

			[OperationContract]
			void Test�Fault();
		}

		public class TestService : ITest
		{
			public void Test()
			{
				throw new Exception();
			}

			public void Test�Fault()
			{
				throw new FaultException<string>("test", new FaultReason("test"));
			}
		}

		[SetUp]
		public void Setup()
		{
			host = new ServiceHost(typeof (TestService));

			var binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			var url = String.Format("net.tcp://{0}:901/Test", Dns.GetHostName());
			host.AddServiceEndpoint(typeof (ITest),
				binding,
				url);

			host.Description.Behaviors.Add(new ErrorHandlerBehavior());
			host.Open();

			var factory = new ChannelFactory<ITest>(binding, url);
			channel = factory.CreateChannel();
		}

		[TearDown]
		public void TearDown()
		{
			((ICommunicationObject)channel).Close();
			host.Close();
		}

		[Test]
		public void Handle_error()
		{
			try
			{
				channel.Test();
				Assert.Fail("�� ��������� ����������");
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Is.EqualTo("��������� ������. ���������� ��������� �������� �������."));
			}
		}

		[Test]
		public void Do_not_handle_fault()
		{
			try
			{
				channel.Test�Fault();
				Assert.Fail("�� ��������� ����������");
			}
			catch (FaultException e)
			{
				Assert.That(e.Message, Is.EqualTo("test"));
			}
		}
	}
}