using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace Inforoom.PriceProcessor
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
#if DEBUG
			Monitor monitor = new Monitor();
			monitor.Start();
			MessageBox.Show("Для остановки нажмите Ok...", "PriceProcessor");
			monitor.Stop();
			monitor = null;
#else
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] 
			{ 
				new PriceProcessorService() 
			};
			ServiceBase.Run(ServicesToRun);
#endif
		}
	}
}
