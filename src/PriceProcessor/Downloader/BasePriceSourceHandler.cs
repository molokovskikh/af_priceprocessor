using System;
using System.IO;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Downloader
{
	/// <summary>
	/// ��������� ������ ��� ������ � PriceProcessItem
	/// </summary>
	public abstract class BasePriceSourceHandler : BaseSourceHandler
	{
		protected void LogDownloaderPrice(string AdditionMessage, DownPriceResultCode resultCode, string ArchFileName, string ExtrFileName)
		{
			var PriceID = Logging(CurrPriceItemId, AdditionMessage, resultCode, ArchFileName, (String.IsNullOrEmpty(ExtrFileName)) ? null : Path.GetFileName(ExtrFileName));
			if (PriceID == 0)
				throw new Exception(String.Format("��� ����������� �����-����� {0} �������� 0 �������� � ID;", CurrPriceItemId));

			CopyToHistory(PriceID);

			if (resultCode != DownPriceResultCode.SuccessDownload)
				return;

			//���� ��� ���������, �� �������� ���� � Inbound
			var NormalName = FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + CurrPriceItemId + "_" + PriceID + GetExt();
			try
			{
				if (File.Exists(NormalName))
					File.Delete(NormalName);
				File.Copy(ExtrFileName, NormalName);
				PriceItemList.AddItem(CreatePriceProcessItem(NormalName));
				using (log4net.NDC.Push(CurrPriceItemId.ToString()))
					_logger.InfoFormat("Price {0} - {1} ������/����������", drCurrent[SourcesTableColumns.colShortName],
									   drCurrent[SourcesTableColumns.colPriceName]);
			}
			catch (Exception ex)
			{
				//todo: �� ���� ����� �� ������ ���������� ������, �� �� ������ ������ ��������, �������� ���� �������� ����������� �������
				using (log4net.NDC.Push(CurrPriceItemId.ToString()))
					_logger.ErrorFormat("�� ������� ��������� ���� '{0}' � ������� '{1}'\r\n{2}", ExtrFileName, NormalName, ex);
			}
		}

		protected abstract void CopyToHistory(UInt64 PriceID);

		protected virtual PriceProcessItem CreatePriceProcessItem(string normalName)
		{
			var item = new PriceProcessItem(true,
			                                Convert.ToUInt64(CurrPriceCode),
			                                CurrCostCode,
			                                CurrPriceItemId,
			                                normalName,
			                                CurrParentSynonym)
			           	{
			           		FileTime = DateTime.Now
			           	};
			return item;
		}
	}
}