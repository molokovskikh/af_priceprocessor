using System;
using System.Collections.Generic;
using System.IO;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
    [Ignore]
	public class FailWaybillParse
	{
		//в списке лежат нужные нам RowId
		private List<uint> rowid = new List<uint>();
		private static readonly ILog _log = LogManager.GetLogger(typeof(WaybillService));

		//директория для сохранения файлов.
		private string DestinationDir = "FailsWaybills";

		[SetUp]
		public void SetUp()
		{
			rowid.Add(7636671);
			rowid.Add(7636653);
			rowid.Add(7636607);
			rowid.Add(7636609);
			rowid.Add(7636601);
			rowid.Add(7636605);
			rowid.Add(7636599);
			rowid.Add(7636553);
			rowid.Add(7636541);
			rowid.Add(7636539);
			rowid.Add(7636533);
			rowid.Add(7636529);
			rowid.Add(7636525);
			rowid.Add(7636523);

			if (!Directory.Exists(DestinationDir))
				Directory.CreateDirectory(DestinationDir);

		}

		[Test]
		public void Parse()
		{
			var logs = DocumentReceiveLog.LoadByIds(rowid.ToArray());
			
			foreach (var log in logs)
			{
				var file = log.GetRemoteFileNameExt();

				try
				{
					var destinationfile = Path.Combine(DestinationDir, Path.GetFileName(file));

					if (File.Exists(file))
					{
						using (new SessionScope())
						{
							File.Copy(file, destinationfile);

							var document = new WaybillFormatDetector().DetectAndParse(log, log.GetFileName());
							if (document == null)
								return;

							using (var transaction = new TransactionScope(OnDispose.Rollback))
							{
								document.Save();
								transaction.VoteCommit();
							}
						}
					}
				}
				catch (Exception e)
				{
					_log.Error(String.Format("Ошибка при разборе документа {0}", file), e);
				}
			}
		}
	}
}
