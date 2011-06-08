using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using Monitor=Inforoom.PriceProcessor.Monitor;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class RestartServiceFixture
	{
		private const string LogInstallPath = @"C:\InstallTestServicePriceProcessor.log";
		private const string ServiceName = @"TestPriceProcessor";
		private const string ExeFileName = @"PriceProcessor.exe";
		private const string DisplayTestServiceName = "Test service PriceProcessor";

		private bool _serviceStopped;

		[SetUp]
		public void Setup()
		{
#if (!DEBUG)
			TestHelper.RecreateDirectories();
			UninstallService();
			InstallService();
#endif
		}

		[Test]
		public void RestartService()
		{
#if DEBUG
			RestartThread(DebugServiceThread);			
#else
			RestartThread(ReleaseServiceThread);
#endif
		}

		public void RestartThread(ThreadStart threadFunc)
		{
			var serviceThread = new Thread(threadFunc);
			serviceThread.Start();
			Thread.Sleep(8000);
			Assert.IsTrue(_serviceStopped, "Ошибка. Служба не завершила свою работу");
			if (!_serviceStopped)
				serviceThread.Abort();			
		}

		private void DebugServiceThread()
		{
			_serviceStopped = false;
			//var monitor = new Monitor();
            var monitor = Monitor.GetInstance();
			monitor.Start();
			Thread.Sleep(2000);
			monitor.Stop();
			_serviceStopped = true;
		}

		private void ReleaseServiceThread()
		{
			_serviceStopped = false;
			StartService();
			Thread.Sleep(2000);
			StopService();
			_serviceStopped = true;			
		}

		private void InstallService()
		{
			var processInstaller = new ServiceProcessInstaller { Account = ServiceAccount.LocalSystem };
			var installer = new ServiceInstaller
			{
				DisplayName = DisplayTestServiceName,
				Description = DisplayTestServiceName,
				ServiceName = ServiceName,
				StartType = ServiceStartMode.Manual
			};
			var context = new InstallContext("", new[] { Path.GetFullPath(ExeFileName) });
			context.Parameters.Add("assemblypath", Path.GetFullPath(ExeFileName));
			installer.Context = context;
			installer.Parent = processInstaller;
			installer.Install(new ListDictionary());
		}

		private static void UninstallService()
		{
			var service = FindService(ServiceName);
			if (service == null)
				return;
			if (service.Status == ServiceControllerStatus.Running)
				service.Stop();

			var installer = new ServiceInstaller
			{
				ServiceName = ServiceName,
				Context = new InstallContext(LogInstallPath, null)
			};
			installer.Uninstall(null);
		}

		private static void StartService()
		{
			var service = FindService(ServiceName);
			service.Start();
		}

		private static void StopService()
		{
			var service = FindService(ServiceName);
			service.Stop();
		}

		private static ServiceController FindService(string name)
		{
			return ServiceController.GetServices().Where(s => s.ServiceName == name).SingleOrDefault();
		}

	}
}
