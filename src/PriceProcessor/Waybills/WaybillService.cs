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

	[ActiveRecord("Document_logs", Schema = "logs")]
	public class DocumentLog : ActiveRecordLinqBase<DocumentLog>
	{
		[PrimaryKey("RowId")]
		public uint Id { get; set; }

		[BelongsTo("FirmCode")]
		public Supplier Supplier { get; set; }

		[Property]
		public uint? ClientCode { get; set; }

		[Property]
		public uint? AddressId { get; set; }

		[Property]
		public DocType DocumentType { get; set; }

		[Property("Addition")]
		public string Comment { get; set; }

		[Property]
		public string FileName { get; set; }

		public string GetFileName()
		{
			var code = AddressId.HasValue ? AddressId.Value : ClientCode;
			var clientDir = Path.Combine(Settings.Default.WaybillsPath, code.ToString().PadLeft(3, '0'));
			var documentDir = Path.Combine(clientDir, DocumentType + "s");
			var file = String.Format("{0}_{1}",
				Id,
				Path.GetFileName(FileName));
			var fullName = Path.Combine(documentDir, file);
			if (!File.Exists(fullName))
			{
				file = String.Format("{0}_{1}({2}){3}",
					Id,
					Supplier.ShortName,
					Path.GetFileNameWithoutExtension(FileName),
					Path.GetExtension(FileName));
				return Path.Combine(documentDir, file);
			}
			else
				return fullName;			
		}

		public static DocumentLog Log(uint supplierId, uint? clientId, uint? addressId, string fileName, DocType documentType, string comment)
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var supplier = Supplier.Find(supplierId);
					var document = new DocumentLog {
						Supplier = supplier,
						AddressId = addressId,
						ClientCode = clientId,
						FileName = fileName,
						DocumentType = documentType,
						Comment = comment,
					};
					document.Create();
					scope.VoteCommit();
					return document;
				}
			}
			catch (Exception e)
			{
				var log = LogManager.GetLogger(typeof (WaybillService));
				log.Error("Ошибка при создании записи в documents.document_log", e);
				return null;
			}
		}
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

		public Document(DocumentLog log)
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

		[BelongsTo("DownloadId")]
		public DocumentLog Log { get; set; }

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
	}

	[ActiveRecord("DocumentBodies", Schema = "documents")]
	public class DocumentLine
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[BelongsTo("DocumentId")]
		public Document Document { get; set; }

		[Property]
		public string Product { get; set; }

		[Property]
		public string Code { get; set; }

		//Информация о сертификате это строка что то вроде РОСС.NL.ФМ09.Д00778
		[Property]
		public string Certificates { get; set; }

		[Property]
		public string Period { get; set; }

		[Property]
		public string Producer { get; set; }

		[Property]
		public string Country { get; set; }

		[Property]
		public decimal? ProducerCost { get; set; }

		[Property]
		public decimal? RegistryCost { get; set; }

		[Property]
		public decimal? SupplierPriceMarkup { get; set; }

		[Property]
		public uint? Nds { get; set; }

		[Property]
		public decimal? SupplierCostWithoutNDS { get; set; }

		[Property]
		public decimal? SupplierCost { get; set; }

		[Property]
		public uint? Quantity { get; set; }

		[Property]
		public bool? VitallyImportant { get; set; }

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
			if (SupplierCost.HasValue && SupplierCostWithoutNDS.HasValue)
				Nds = (uint?) (Math.Round((SupplierCost.Value/SupplierCostWithoutNDS.Value - 1)*100));
		}

		public void SetSupplierCostByNds(decimal? nds)
		{
			Nds = (uint?) nds;
			SupplierCost = null;
			if (SupplierCostWithoutNDS.HasValue && Nds.HasValue)
				SupplierCost = Math.Round(SupplierCostWithoutNDS.Value*(1 + ((decimal) Nds.Value/100)), 2);
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
					var detector = new WaybillFormatDetector();
					var docsForParsing = MultifileDocument.Merge(ids);

					var docs = docsForParsing.Select(d => {
						try
						{
							if (d.DocumentLog.DocumentType.Equals(DocType.Reject))
								return null;
							var parser = detector.DetectParser(d.FileName, d.DocumentLog);
							return parser.Parse(d.FileName, new Document(d.DocumentLog));
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

					using (new TransactionScope())
					{
						docs.Each(d => d.Save());
						return docs.Select(d => d.Id).ToArray();
					}
				}
			}
			catch (Exception e)
			{
				_log.Error("Ошибка при разборе накладных", e);
			}
			return new uint[0];
		}

		public static void SaveWaybill(string filename)
		{
			if (!Directory.Exists(Settings.Default.DownWaybillsPath))
				Directory.CreateDirectory(Settings.Default.DownWaybillsPath);

			if (File.Exists(filename))
				File.Copy(filename, Path.Combine(Settings.Default.DownWaybillsPath, Path.GetFileName(filename)), true);
		}

		public static void ParserDocument(uint documentLogId, string file)
		{
			try
			{
				using(new SessionScope())
				{
					var log = ActiveRecordBase<DocumentLog>.Find(documentLogId);
					var settings = ActiveRecordBase<WaybillSettings>.Find(log.ClientCode.Value);
					if (!settings.ShouldParseWaybill() || (log.DocumentType == DocType.Reject))
						return;

					var detector = new WaybillFormatDetector();
					var parser = detector.DetectParser(file, log);
					var document = new Document(log);
					parser.Parse(file, document);
					if (!document.DocumentDate.HasValue)
						document.DocumentDate = DateTime.Now;
					using (new TransactionScope())
						document.Save();
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
