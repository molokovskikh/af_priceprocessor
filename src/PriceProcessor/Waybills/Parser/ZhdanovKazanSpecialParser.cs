using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	// Парсер для ИП Жданов (требование 3657). Формат похож на GenesisDbfParser, но для срока годности используется поле DUE_DATE.
	public class ZhdanovKazanSpecialParser : IDocumentParser
	{
		public static DataTable Load(string file)
		{
			try
			{
				return Dbf.Load(file);
			}
			catch (DbfException)
			{
				return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			}
		}

		public Document Parse(string file, Document document)
		{			
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "TRX_NUM")
				.DocumentHeader(d => d.DocumentDate, "TRX_DATE")
				.Line(l => l.Code, "ITEM_ID")
				.Line(l => l.Product, "ITEM_NAME")
				.Line(l => l.Producer, "VEND_NAME")
				.Line(l => l.ProducerCost, "PRICE_VR")
				.Line(l => l.Nds, "TAX_RATE")				
				.Line(l => l.Quantity, "QNTY")
				.Line(l => l.Period, "DUE_DATE")				
				.Line(l => l.SerialNumber, "LOT_NUMBER")
				.Line(l => l.Certificates, "CER_NUMBER")				
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.SupplierCost, "PRICE_TAX")
				.Line(l => l.Amount, "FULL_AMNT")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("TRX_DATE") &&
				table.Columns.Contains("PRICE_VR") &&
				table.Columns.Contains("QNTY") &&
				table.Columns.Contains("DUE_DATE") &&
				table.Columns.Contains("PRICE") &&
				table.Columns.Contains("PRICE_TAX") &&
				table.Columns.Contains("FULL_AMNT");
		}
	}
}
