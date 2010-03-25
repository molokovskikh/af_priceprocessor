using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills.Parser;
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
	public class Supplier
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

		[Property]
		public string FileName { get; set; }

		public string GetFileName()
		{
			var clientDir = Path.Combine(Settings.Default.WaybillsPath, ClientCode.ToString().PadLeft(3, '0'));
			var documentDir = Path.Combine(clientDir, DocumentType + "s");
/*			var file = String.Format("{0}_{1}({2}){3}",
				Id,
				Supplier.ShortName,
				Path.GetFileNameWithoutExtension(FileName),
				Path.GetExtension(FileName));*/
			var file = String.Format("{0}_{1}",
				Id,
				Path.GetFileName(FileName));
			return Path.Combine(documentDir, file);
		}
	}

	[ActiveRecord("RetClientsSet", Schema = "Usersettings")]
	public class WaybillSettings : ActiveRecordLinqBase<WaybillSettings>
	{
		[PrimaryKey("ClientCode")]
		public uint Id { get; set; }

		[Property]
		public bool ParseWaybills { get; set; }
	}

	public class WaybillFormatDetector
	{
		public IDocumentParser DetectParser(string file)
		{
			var extention = Path.GetExtension(file.ToLower());
			Type type = null;
			if (extention == ".dbf")
				type = typeof (SiaParser);
			else if (extention == ".sst")
				type = typeof (UkonParser);
			else if (extention == ".xml")
			{
				if (new SiaXmlParser().IsInCorrectFormat(file))
					type = typeof (SiaXmlParser);
				else if (new ProtekXmlParser().IsInCorrectFormat(file))
					type = typeof (ProtekXmlParser);
			}
			else if (extention == ".pd")
				type = typeof (ProtekParser);

			if (type == null)
				return null;

			var constructor = type.GetConstructors().Where(c => c.GetParameters().Count() == 0).FirstOrDefault();
			if (constructor == null)
				throw new Exception("У типа {0} нет конструктора без аргументов");
			return (IDocumentParser)constructor.Invoke(new object[0]);
		}
	}

	public enum DocType
	{
		Waybill = 1,
		Reject = 2
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
			FirmCode = log.Supplier.Id;
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

		public void SetNds(decimal nds)
		{
			SupplierCostWithoutNDS = Math.Round(SupplierCost.Value/(1 + nds/100), 2);
			Nds = (uint?) nds;
		}

		public void SetProducerCostWithoutNds(decimal cost)
		{
			SupplierCostWithoutNDS = cost;
			Nds = (uint?)(Math.Round((SupplierCost.Value / SupplierCostWithoutNDS.Value - 1) * 100));
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
					var documents = ids.Select(id => ActiveRecordBase<DocumentLog>.Find(id)).ToList();
					var detector = new WaybillFormatDetector();
					var docs = documents.Select(d => {
						try
						{
							var parser = detector.DetectParser(d.GetFileName());
							if (parser == null)
								return null;
							return parser.Parse(d.GetFileName(), new Document(d));
						}
						catch(Exception e)
						{
							var filename = d.GetFileName();
							_log.Error(String.Format("Не удалось разобрать накладную {0}", filename), e);
							SaveWaybill(filename);
							return null;
						}
					}).Where(d => d != null).ToList();

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
				File.Copy(filename, Path.Combine(Settings.Default.DownWaybillsPath, Path.GetFileName(filename)));
		}

		public static void ParserDocument(uint documentLogId, string file)
		{
			try
			{
				using(new SessionScope())
				{
					var log = ActiveRecordBase<DocumentLog>.Find(documentLogId);
					var settings = ActiveRecordBase<WaybillSettings>.Find(log.ClientCode.Value);
					if (!settings.ParseWaybills)
						return;

					var detector = new WaybillFormatDetector();
					var parser = detector.DetectParser(file);
					if (parser == null)
						return;
					var document = new Document(log);
					parser.Parse(file, document);
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
