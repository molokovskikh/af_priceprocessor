using System;
using System.Data;
using System.Data.OleDb;
using System.Threading;
using Inforoom.PriceProcessor.Properties;
using log4net;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class OleDbHelper
	{
		private readonly static ILog _log = LogManager.GetLogger(typeof (OleDbHelper));

		public static DataTable FillPrice(OleDbDataAdapter da)
		{
			var tryCount = 0;
			do
			{
				try
				{
					var table = new DataTable();
					da.Fill(table);
					return table;
				}
				catch(System.Runtime.InteropServices.InvalidComObjectException)
				{
					if (tryCount < Settings.Default.MinRepeatTranCount)
					{
						tryCount++;
						_log.Error("Repeat Fill dtPrice on InvalidComObjectException");
						Thread.Sleep(500);
					}
					else
						throw;
				}
				catch(NullReferenceException)
				{
					if (tryCount < Settings.Default.MinRepeatTranCount)
					{
						tryCount++;
						_log.Error("Repeat Fill dtPrice on NullReferenceException");
						Thread.Sleep(500);
					}
					else
						throw;
				}
			}while(true);
		}
	}
}
