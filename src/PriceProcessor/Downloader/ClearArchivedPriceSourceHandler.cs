using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Inforoom.Downloader.Properties;
using System.Threading;

namespace Inforoom.Downloader
{
	class ClearArchivedPriceSourceHandler : BaseSourceHandler
	{
		//����� ���������� ������������ ��������
		private DateTime lastScan;

		public ClearArchivedPriceSourceHandler()
            : base()
        {
			lastScan = DateTime.MinValue;
        }

		protected override void ProcessData()
		{
			try
			{
				//��������� ����� ��������� �����
				if (DateTime.Now.Subtract(lastScan).TotalHours > Settings.Default.ClearScanInterval)
				{
					string[] archivedPrices = Directory.GetFiles(DownHistoryPath);

					foreach (string priceFile in archivedPrices)
					{
						DateTime fileLastWrite = File.GetLastWriteTime(priceFile);

						//���� �������� � ���� ������ ��� � ���������, �� ���� �������
						if (DateTime.Now.Subtract(fileLastWrite).TotalDays > Settings.Default.DepthOfStorageArchivePrices)
							File.Delete(priceFile);
					}

					lastScan = DateTime.Now;
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex)
			{
				LoggingToService(ex.ToString());
			}
		}

	}
}
