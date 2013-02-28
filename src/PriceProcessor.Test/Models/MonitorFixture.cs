using System;
using System.Collections.Generic;
using System.Threading;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using log4net.Config;
using log4net.Core;
using Monitor = Inforoom.PriceProcessor.Monitor;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class MonitorFixture
	{
		private EventFilter<Monitor> events;

		public class TestHandler : BaseSourceHandler
		{
			public DateTime Started;
			public ManualResetEvent Aborted = new ManualResetEvent(false);
			public static ManualResetEvent New = new ManualResetEvent(false);
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
				New.Set();
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
					Aborted.Set();
				}
			}
		}

		[SetUp]
		public void Setup()
		{
			events = new EventFilter<Monitor>(Level.Debug);
			new EventFilter<TestHandler>(Level.Debug, events.FakeAppender);
		}

		[TearDown]
		public void Teardown()
		{
			events.Reset();
		}

		[Test]
		public void Restart_handle()
		{
			var testHandler = new TestHandler(false);
			TestHandler.New.Reset();

			var monitor = new Monitor(testHandler);
			monitor.StopWaitTimeout = 200;
			monitor.Start();

			testHandler.Aborted.WaitOne(1000);
			TestHandler.New.WaitOne(1000);

			monitor.Stop();
			Assert.That(TestHandler.Handlers.Count, Is.EqualTo(2), events.Events.Implode(e => e.MessageObject));
			Assert.That(TestHandler.Handlers[1].Started, Is.GreaterThan(DateTime.MinValue));
		}
	}
}