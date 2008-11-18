﻿using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Inforoom.PriceProcessor
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			//конфигурация log4net
			log4net.Config.XmlConfigurator.ConfigureAndWatch(
				new FileInfo(
					Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
					Path.DirectorySeparatorChar + 
					"log4net.config"));
			//устанавливаем значение NullText для параметра %ndc и других
			log4net.Util.SystemInfo.NullText = null;

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
