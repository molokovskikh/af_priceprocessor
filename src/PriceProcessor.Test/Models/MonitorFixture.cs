using System;
using System.Collections.Generic;
using System.Threading;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using log4net.Config;
using Monitor = Inforoom.PriceProcessor.Monitor;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class MonitorFixture
	{
		public class TestHandler : BaseSourceHandler
		{
			public DateTime Started;
			public ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
			public static List<TestHandler> Handlers = new List<TestHandler>();

			private bool normal;

			public TestHandler()
				: this(true)
			{
			}

			public TestHandler(bool normal)
			{
				JoinTimeout = 200;
				this.normal = normal;
				Handlers.Add(this);
			}

			public override void ProcessData()
			{
				Started = DateTime.Now;
				try {
					if (!normal)
						lastPing = DateTime.MinValue;

					while (true) {
						Thread.Sleep(50);
					}
				}
				finally {
					//clr не будет кидать thread abort exception
					//если мы сидим в finaly
					if (!normal)
						ManualResetEvent.WaitOne();
				}
			}
		}

		[Test]
		public void Restart_handle()
		{
			var testHandler = new TestHandler(false);
			var monitor = new Monitor(testHandler);
			monitor.StopWaitTimeout = 200;
			monitor.Start();
			Thread.Sleep(1000);

			testHandler.ManualResetEvent.Set();
			monitor.Stop();
			Assert.That(TestHandler.Handlers.Count, Is.EqualTo(2));
			Assert.That(TestHandler.Handlers[1].Started, Is.GreaterThan(DateTime.MinValue));
		}
	}
}