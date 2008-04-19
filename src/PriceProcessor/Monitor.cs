using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Inforoom.Logging;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Formalizer;

namespace Inforoom.PriceProcessor
{
	/// <summary>
	/// ����� ��� ������������ ������ ������������ ����������.
	/// </summary>
	public class Monitor
	{
		private List<AbstractHandler> alHandlers = null;
		private bool Stopped = false;

		private Thread tMonitor;


		public Monitor()
		{
			alHandlers = new List<AbstractHandler>();
			//alHandlers.Add(new LANSourceHandler());
			//alHandlers.Add(new FTPSourceHandler());
			//alHandlers.Add(new HTTPSourceHandler());
			//alHandlers.Add(new EMAILSourceHandler());
			//alHandlers.Add(new WaybillSourceHandler());
			//alHandlers.Add(new WaybillLANSourceHandler());
			//alHandlers.Add(new ClearArchivedPriceSourceHandler());
			alHandlers.Add(new FormalizeHandler());
			tMonitor = new Thread(new ThreadStart(MonitorWork));
		}

		//��������� ������� � �������������
		public void Start()
		{
            try
            {
				foreach (AbstractHandler handler in alHandlers)
                    try
                    {
                        handler.StartWork();
                    }
                    catch(Exception exHan)
                    {
                        SimpleLog.Log("Monitor.Start." + handler.GetType().Name, "������ ��� ������ ����������� : {0}", exHan);
                    }
                tMonitor.Start();
				SimpleLog.Log("Monitor", "PriceProcessor �������.");
			}
            catch (Exception ex)
            {
                SimpleLog.Log("Monitor.Start", "������ ��� ������ �������� : {0}", ex);
            }
		}

		//������������� �������
		public void Stop()
		{
            try
            {
                Stopped = true;
                System.Threading.Thread.Sleep(3000);
                tMonitor.Abort();
				foreach (AbstractHandler handler in alHandlers)
                    try
                    {
                        handler.StopWork();
                    }
                    catch (Exception exHan)
                    {
                        SimpleLog.Log("Monitor.Stop." + handler.GetType().Name, "������ ��� �������� ����������� : {0}", exHan);
                    }
            }
            catch (Exception ex)
            {
                SimpleLog.Log("Monitor.Stop", "������ ��� �������� �������� : {0}", ex);
            }
			SimpleLog.Log("Monitor", "PriceProcessor ����������.");
        }

		private void MonitorWork()
		{
			while (!Stopped)
				try
				{
					foreach(AbstractHandler handler in alHandlers)
					{
						if (!handler.Worked)
							handler.RestartWork();
					}

					System.Threading.Thread.Sleep(500);
				}
				catch(Exception e)
				{
                    SimpleLog.Log("Monitor", "������ � ����� : {0}", e);
				}
		}
	}
}
