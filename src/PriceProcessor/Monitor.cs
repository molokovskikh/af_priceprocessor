using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Inforoom.Logging;

namespace Inforoom.Downloader
{
	/// <summary>
	/// ����� ��� ������������ ������ ������������ ����������.
	/// </summary>
	public class Monitor
	{
		private List<BaseSourceHandler> alHandlers = null;
		private bool Stopped = false;

		private Thread tMonitor;


		public Monitor()
		{
			alHandlers = new List<BaseSourceHandler>();
			alHandlers.Add(new LANSourceHandler());
			alHandlers.Add(new FTPSourceHandler());
			alHandlers.Add(new HTTPSourceHandler());
			alHandlers.Add(new EMAILSourceHandler());
			alHandlers.Add(new WaybillSourceHandler());
			alHandlers.Add(new WaybillLANSourceHandler());
			alHandlers.Add(new ClearArchivedPriceSourceHandler());
            tMonitor = new Thread(new ThreadStart(MonitorWork));
		}

		//��������� ������� � �������������
		public void Start()
		{
            try
            {
                SimpleLog.Log("Monitor", "Downloader started.");
                foreach (BaseSourceHandler bsh in alHandlers)
                    try
                    {
                        bsh.StartWork();
                    }
                    catch(Exception exHan)
                    {
                        SimpleLog.Log("Monitor.Start." + bsh.GetType().Name, "������ ��� ������ ����������� : {0}", exHan);
                    }
                tMonitor.Start();
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
                foreach (BaseSourceHandler bs in alHandlers)
                    try
                    {
                        bs.StopWork();
                    }
                    catch (Exception exHan)
                    {
                        SimpleLog.Log("Monitor.Stop." + bs.GetType().Name, "������ ��� �������� ����������� : {0}", exHan);
                    }
            }
            catch (Exception ex)
            {
                SimpleLog.Log("Monitor.Stop", "������ ��� �������� �������� : {0}", ex);
            }
            SimpleLog.Log("Monitor", "Downloader stoppped.");
        }

		private void MonitorWork()
		{
			while (!Stopped)
				try
				{
					foreach(BaseSourceHandler bs in alHandlers)
					{
						if (!bs.Worked)
							bs.RestartWork();
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
