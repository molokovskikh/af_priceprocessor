using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using log4net;

namespace Inforoom.PriceProcessor.Waybills
{
	[ServiceContract]
	public interface IWaybillService
	{
		[OperationContract]
		uint[] ParseWaybill(uint[] uints);
	}

	[ActiveRecord("Clientsdata", Schema = "Usersettings")]
	public class Supplier : ActiveRecordLinqBase<Supplier>
	{
		[PrimaryKey("FirmCode")]
		public uint Id { get; set; }

		[Property]
		public string ShortName { get; set; }
	}

	[ActiveRecord("RetClientsSet", Schema = "Usersettings")]
	public class WaybillSettings : ActiveRecordLinqBase<WaybillSettings>
	{
		[PrimaryKey("ClientCode")]
		public uint Id { get; set; }

		[Property]
		public bool ParseWaybills { get; set; }

		[Property]
		public bool OnlyParseWaybills { get; set; }


		public bool ShouldParseWaybill()
		{
			return ParseWaybills || OnlyParseWaybills;
		}
	}

	public enum DocType
	{
		[Description("Накладная")] Waybill = 1,
		[Description("Отказ")] Reject = 2
	}

	[ActiveRecord("DocumentHeaders", Schema = "documents")]
	public class Document : ActiveRecordLinqBase<Document>
	{
		public Document()
		{}

		public Document(DocumentReceiveLog log)
		{
			Log = log;
			WriteTime = DateTime.Now;
			FirmCode = Convert.ToUInt32(log.Supplier.Id);
			ClientCode = log.ClientCode.Value;
			AddressId = log.AddressId;
			DocumentType = DocType.Waybill;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public DateTime WriteTime { get; set; }

		[Property]
		public uint FirmCode { get; set; }

		[Property]
		public uint ClientCode { get; set; }

		[Property]
		public uint? AddressId { get; set; }

		[Property]
		public DocType DocumentType { get; set; }

		[Property]
		public string ProviderDocumentId { get; set; }

		[Property]
		public DateTime? DocumentDate { get; set; }

		[Property]
		public string Parser { get; set; }

		[Property]
		public uint? OrderId { get; set; }

		[BelongsTo("DownloadId")]
		public DocumentReceiveLog Log { get; set; }

		[HasMany(ColumnKey = "DocumentId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<DocumentLine> Lines { get; set; }

		public DocumentLine NewLine()
		{
			return NewLine(new DocumentLine());
		}

		public DocumentLine NewLine(DocumentLine line)
		{
			if (Lines == null)
				Lines = new List<DocumentLine>();

			line.Document = this;
			Lines.Add(line);
			return line;
		}

		public void Parse(IDocumentParser parser, string file)
		{
			Parser = parser.GetType().Name;
			parser.Parse(file, this);
			if (!DocumentDate.HasValue)
				DocumentDate = DateTime.Now;
		}

		public static string GenerateProviderDocumentId()
		{
			return DateTime.Now.ToString()
				.Replace(".", String.Empty)
				.Replace(" ", String.Empty)
				.Replace(":", String.Empty)
				.Replace(",", String.Empty)
				.Replace("-", String.Empty)
				.Replace("/", String.Empty);
		}
	}

	[ActiveRecord("DocumentBodies", Schema = "documents")]
	public class DocumentLine
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("DocumentId")]
		public Document Document { get; set; }

		/// <summary>
		/// Наименование продукта
		/// </summary>
		[Property]
		public string Product { get; set; }

		/// <summary>
		/// Код товара
		/// </summary>
		[Property]
		public string Code { get; set; }

		/// <summary>
		/// Информация о сертификате это строка что то вроде РОСС.NL.ФМ09.Д00778
		/// </summary>
		[Property]
		public string Certificates { get; set; }

		/// <summary>
		/// Срок годности
		/// </summary>
		[Property]
		public string Period { get; set; }

		/// <summary>
		/// Производитель
		/// </summary>
		[Property]
		public string Producer { get; set; }

		/// <summary>
		/// Страна производителя
		/// </summary>
		[Property]
		public string Country { get; set; }

		/// <summary>
		/// Цена производителя без НДС
		/// </summary>
		[Property]
		public decimal? ProducerCost { get; set; }

		/// <summary>
		/// Цена государственного реестра
		/// </summary>
		[Property]
		public decimal? RegistryCost { get; set; }

		/// <summary>
		/// Наценка поставщика
		/// </summary>
		[Property]
		public decimal? SupplierPriceMarkup { get; set; }

		/// <summary>
		/// Ставка налога на добавленную стоимость
		/// </summary>
		[Property]
		public uint? Nds { get; set; }

		/// <summary>
		/// Цена поставщика без НДС
		/// </summary>
		[Property]
		public decimal? SupplierCostWithoutNDS { get; set; }

		/// <summary>
		/// Цена поставщика с НДС
		/// </summary>
		[Property]
		public decimal? SupplierCost { get; set; }

		/// <summary>
		/// Количество
		/// </summary>
		[Property]
		public uint? Quantity { get; set; }

		/// <summary>
		/// Признак ЖНВЛС
		/// </summary>
		[Property]
		public bool? VitallyImportant { get; set; }

		/// <summary>
		/// Серийный номер продукта
		/// </summary>
		[Property]
		public string SerialNumber { get; set; }

		public void SetValues()
		{
			if (!Nds.HasValue && SupplierCostWithoutNDS.HasValue)
				SetSupplierCostWithoutNds(SupplierCostWithoutNDS.Value);
			if (!SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SetNds(Nds.Value);
			if (!SupplierCost.HasValue && Nds.HasValue && SupplierCostWithoutNDS.HasValue)
				SetSupplierCostByNds(Nds.Value);
		}

		public void SetNds(decimal nds)
		{
			SupplierCostWithoutNDS = null;
			if (SupplierCost.HasValue)
				SupplierCostWithoutNDS = Math.Round(SupplierCost.Value/(1 + nds/100), 2);
			Nds = (uint?) nds;
		}

		public void SetSupplierCostWithoutNds(decimal cost)
		{
			SupplierCostWithoutNDS = cost;
			Nds = null;
			if (SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue && (SupplierCostWithoutNDS.Value != 0))
				Nds = (uint?) (Math.Round((SupplierCost.Value/SupplierCostWithoutNDS.Value - 1)*100));
		}

		public void SetSupplierCostByNds(decimal? nds)
		{
			Nds = (uint?) nds;
			SupplierCost = null;
			if (SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SupplierCost = Math.Round(SupplierCostWithoutNDS.Value*(1 + ((decimal) Nds.Value/100)), 2);
		}

		public void SetSupplierPriceMarkup()
		{
			if (!SupplierPriceMarkup.HasValue && ProducerCost.HasValue
				&& SupplierCostWithoutNDS.HasValue && (ProducerCost.Value != 0))
			{
				SupplierPriceMarkup = null;
				SupplierPriceMarkup = Math.Round(((SupplierCostWithoutNDS.Value/ProducerCost.Value - 1)*100), 2);
			}
		}
	}

	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
	public class WaybillService : IWaybillService
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (WaybillService));

		public uint[] ParseWaybill(uint[] ids)
		{
			try
			{
				using (new SessionScope())
				{
					return ParseWaybills(DocumentReceiveLog.LoadByIds(ids), false)
						.Select(d => d.Id)
						.ToArray();
				}
			}
			catch (Exception e)
			{
				_log.Error("Ошибка при разборе накладных", e);
			}
			return new uint[0];
		}

		public static void ParseWaybills(List<DocumentReceiveLog> logs)
		{
			try
			{
				ParseWaybills(logs, true);
			}
			catch(Exception e)
			{
				_log.Error("Ошибка при разборе накладных", e);
			}
		}

		private static IEnumerable<Document> ParseWaybills(List<DocumentReceiveLog> logs, bool shouldCheckClientSettings)
		{
			var detector = new WaybillFormatDetector();
			var docsForParsing = MultifileDocument.Merge(logs);

			var docs = docsForParsing.Select(d => {
                
                try
				{
					var settings = WaybillSettings.Find(d.DocumentLog.ClientCode.Value);

					if (d.DocumentLog.DocumentType == DocType.Reject)
						return null;
					if (shouldCheckClientSettings && !settings.ShouldParseWaybill())
						return null;
					return detector.DetectAndParse(d.DocumentLog, d.FileName);
				}
				catch (Exception e)
				{
				    var filename = d.FileName;
					_log.Error(String.Format("Не удалось разобрать накладную {0}", filename), e);
					SaveWaybill(filename);
					return null;
				}
			}).Where(d => d != null).ToList();
			MultifileDocument.DeleteMergedFiles(docsForParsing);

			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				docs.Each(d => d.Save());
				scope.VoteCommit();
			}
			return docs;
		}

		public static void SaveWaybill(string filename)
		{
			if (!Directory.Exists(Settings.Default.DownWaybillsPath))
				Directory.CreateDirectory(Settings.Default.DownWaybillsPath);

            if (File.Exists(filename))
                File.Copy(filename, Path.Combine(Settings.Default.DownWaybillsPath, Path.GetFileName(filename)), true);
		}

		public static void ParserDocument(DocumentReceiveLog log)
		{
			ParserDocument(log.Id, log.GetFileName());
		}

		public static void ParserDocument(uint documentLogId, string file)
		{
			try
			{
				using(new SessionScope())
				{
					var log = DocumentReceiveLog.Find(documentLogId);
					var settings = WaybillSettings.Find(log.ClientCode.Value);
					if (!settings.ShouldParseWaybill() || (log.DocumentType == DocType.Reject))
						return;

					var document = new WaybillFormatDetector().DetectAndParse(log, file);
					if (document == null)
						return;
					using (var transaction = new TransactionScope(OnDispose.Rollback))
					{

                            document.Save();
                            transaction.VoteCommit();
                    }
				}
			}
			catch(Exception e)
			{
				_log.Error(String.Format("Ошибка при разборе документа {0}", file), e);
				SaveWaybill(file);
			}
		}
	}
}
