using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

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
