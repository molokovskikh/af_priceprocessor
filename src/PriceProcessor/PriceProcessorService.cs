using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

namespace Inforoom.PriceProcessor
{
	public partial class PriceProcessorService : ServiceBase
	{
		private Monitor monitor = null;

		public PriceProcessorService()
		{
			InitializeComponent();
			//monitor = new Monitor();
			monitor = Monitor.GetInstance();
		}

		protected override void OnStart(string[] args)
		{
			monitor.Start();
		}

		protected override void OnStop()
		{
			monitor.Stop();
		}
	}
}