using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Downloader
{
	[ServiceContract(Namespace="http://service.ezakaz.protek.ru")]
	public interface ProtekService
	{
		[OperationContract(Action = "urn:getBladingHeaders", ReplyAction = "urn:getBladingHeadersResponse")]
		[XmlSerializerFormat(SupportFaults = true)]
		[return: MessageParameter(Name = "return")]
		getBladingHeadersResponse getBladingHeaders(getBladingHeadersRequest request);

		[OperationContract(Action = "urn:getBladingBody", ReplyAction = "urn:getBladingBodyResponse")]
		[XmlSerializerFormat(SupportFaults = true)]
		[return: MessageParameter(Name = "return")]
		getBladingBodyResponse getBladingBody(getBladingBodyRequest request);

		[OperationContract(Action = "urn:closeBladingSession", ReplyAction = "urn:closeBladingSessionResponse")]
		[XmlSerializerFormat(SupportFaults = true)]
		[return: MessageParameter(Name = "return")]
		closeBladingSessionResponse closeBladingSession(closeBladingSessionRequest request);

		[OperationContract(Action = "urn:getSertImages", ReplyAction = "urn:getSertImagesResponse")]
		[XmlSerializerFormat(SupportFaults = true)]
		[return: MessageParameter(Name = "return")]
		getSertImagesResponse getSertImages(getSertImagesRequest request);

		[OperationContractAttribute(Action="urn:getSertDocType", ReplyAction="urn:getSertDocTypeResponse")]
		[XmlSerializerFormatAttribute(SupportFaults=true)]
		[return: MessageParameterAttribute(Name="return")]
		getSertDocTypeResponse getSertDocType(getSertDocTypeRequest request);
	}

	[MessageContract(WrapperName="getSertDocTypeResponse", WrapperNamespace="http://service.ezakaz.protek.ru", IsWrapped=true)]
	public class getSertDocTypeResponse
	{
		[MessageBodyMember(Namespace="http://service.ezakaz.protek.ru")]
		[XmlElement(Form=XmlSchemaForm.Unqualified, IsNullable=true)]
		public EZakazXML @return;

		public getSertDocTypeResponse()
		{}

		public getSertDocTypeResponse(EZakazXML @return)
		{
			this.@return = @return;
		}
	}

	[MessageContract(WrapperName="getSertDocType", WrapperNamespace="http://service.ezakaz.protek.ru", IsWrapped=true)]
	public class getSertDocTypeRequest
	{
		[MessageBodyMember(Namespace="http://service.ezakaz.protek.ru")]
		[XmlElement(Form=XmlSchemaForm.Unqualified)]
		public int clientId;

		[MessageBodyMember(Namespace="http://service.ezakaz.protek.ru")]
		[XmlElement(Form=XmlSchemaForm.Unqualified)]
		public int instCode;

		public getSertDocTypeRequest()
		{}

		public getSertDocTypeRequest(int clientId, int instCode)
		{
			this.clientId = clientId;
			this.instCode = instCode;
		}
	}

	[MessageContract(WrapperName = "getSertImagesResponse", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class getSertImagesResponse
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru")]
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public EZakazXML @return;

		public getSertImagesResponse()
		{}

		public getSertImagesResponse(EZakazXML @return)
		{
			this.@return = @return;
		}
	}

	[MessageContract(WrapperName = "getSertImages", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class getSertImagesRequest
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru")]
		[XmlElementAttribute(Form = XmlSchemaForm.Unqualified)]
		public int clientId;

		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru")]
		[XmlElement(Form = XmlSchemaForm.Unqualified)] public int instCode;

		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru")]
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int theDocId;

		public getSertImagesRequest()
		{}

		public getSertImagesRequest(int clientId, int instCode, int theDocId)
		{
			this.clientId = clientId;
			this.instCode = instCode;
			this.theDocId = theDocId;
		}
	}

	[ActiveRecord("OrdersHead", Schema = "Orders")]
	public class OrderHead : ActiveRecordLinqBase<OrderHead>
	{
		[PrimaryKey("RowId")]
		public virtual uint Id { get; set; }

		[Property]
		public DateTime WriteTime { get; set; }

		[BelongsTo("AddressId")]
		public virtual Address Address { get; set; }

		[Property]
		public virtual uint ClientCode { get; set; }

		[BelongsTo("PriceCode")]
		public virtual Price Price { get; set; }

		[HasMany(ColumnKey = "OrderId", Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<OrderItem> Items { get; set; }
	}

	[ActiveRecord("OrdersList", Schema = "Orders")]
	public class OrderItem : ActiveRecordLinqBase<OrderItem>
	{
		[PrimaryKey("RowId")]
		public uint Id { get; set; }

		[Property]
		public uint? Quantity { get; set; }

		[Property]
		public ulong? CoreId { get; set; }

		[Property]
		public float? Cost { get; set; }

		[Property]
		public string Code { get; set; }

		[BelongsTo("OrderId")]
		public OrderHead Order { get; set; }
	}

	public class ProtekServiceConfig
	{
		public string Url;
		public int ClientId;
		public int InstanceId;
		public uint SupplierId;

		public ProtekServiceConfig(string url, int clientId, int instanceId, uint supplierId)
		{
			Url = url;
			ClientId = clientId;
			InstanceId = instanceId;
			SupplierId = supplierId;
		}
	}

	public class ProtekWaybillHandler : AbstractHandler
	{
		private string uri;
		private IList<OrderHead> orders = new List<OrderHead>();

		public static List<ProtekServiceConfig> Configs = new List<ProtekServiceConfig> {
			//калуга
			new ProtekServiceConfig("http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				79888, 1024847, 3287),

			//воронеж
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				123108, 1064974, 5),

			//Курск/Белгород
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				118855, 1053374, 220),

			//тамбов
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				261544, 1072996, 2399),

			//Москва
			new ProtekServiceConfig("http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				83674, 1033812, 180),

			//Смоленск
			new ProtekServiceConfig("http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				83868, 1033815, 2495),

			//Казань
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				231691, 1072909, 2777),

			//Екатеринбург
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				261542, 1072994, 3752),

			//Пермь
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				215115, 1072912, 7114),

			//Челябинск
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				261543, 1072995, 3),

			//Киров
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				215116, 1072913, 7743),

			//Орел
			new ProtekServiceConfig("http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				83472, 1033813, 5375),

			//Тюмень
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				250434, 1072911, 7088),

			//ХМАО-Югра
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				245011, 1072914, 7740),

			//Омск
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				265488, 1077902, 3777),

			//Протек-02 Волгоград
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				266327, 1079618, 4166),

			//Протек-12 Самара
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				266329, 1079620, 3745),

			//Протек-16 Новосибирск
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				266337, 1079622, 4631),

			//Протек-05 Нижний Новгород
			new ProtekServiceConfig("http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				86980, 1036488, 3444),

			//Протек-36 Оренбург
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				266508, 1080034, 3496),

			//Протек-17 Уфы
			new ProtekServiceConfig("http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/",
				274265, 1091749, 12297),
		};

		public virtual void WithService(string uri, Action<ProtekService> action)
		{
			var endpoint = new EndpointAddress(uri);
			var binding = new BasicHttpBinding {
				SendTimeout = TimeSpan.FromMinutes(10),
				ReceiveTimeout = TimeSpan.FromMinutes(10),
				MaxBufferPoolSize = 30*1024*1024,
				MaxBufferSize = 10*1024*1024,
				MaxReceivedMessageSize = 10*1024*1024
			};
			var factory = new ChannelFactory<ProtekService>(binding, endpoint);
			var service = factory.CreateChannel();
			var communicationObject = ((ICommunicationObject)service);
			try
			{
				action(service);
				communicationObject.Close();
			}
			catch (FaultException e)
			{
				_logger.Warn("Ошибка в сервисе протека", e);
			}
			catch (Exception)
			{
				if (communicationObject.State != CommunicationState.Closed)
					communicationObject.Abort();
				throw;
			}
		}

		protected override void ProcessData()
		{
			foreach (var config in Configs) {
				uri = config.Url;
				Load(config.ClientId, config.InstanceId);
			}
		}

		protected void Load(int clientId, int instanceId)
		{
			WithService(uri, service => {
				_logger.InfoFormat("Запрос накладных, clientId = {0} instanceId = {1}", clientId, instanceId);
				var responce = service.getBladingHeaders(new getBladingHeadersRequest(clientId, instanceId));
				var sessionId = responce.@return.wsSessionIdStr;

				try
				{
					if (responce.@return.blading == null)
						return;

					_logger.InfoFormat("Получили накладные, всего {0} для сессии {1}", responce.@return.blading.Length, sessionId);
					foreach (var blading in responce.@return.blading)
					{
						var blanding = service.getBladingBody(new getBladingBodyRequest(sessionId, clientId, instanceId, blading.bladingId.Value));
						_logger.InfoFormat("Загрузил накладную {0}", blading.bladingId.Value);
						foreach (var body in blanding.@return.blading)
						{
							using (var scope = new TransactionScope(OnDispose.Rollback))
							{
								var document = ToDocument(body);
								if (document == null)
									continue;

								var settings = WaybillSettings.Find(document.ClientCode);

								CertificateSourceDetector.DetectAndParse(document);
								document.Log.Save();
								document.Save();
								document.CreateCertificateTasks();

								if (!DbfExporter.ConvertAndSaveDbfFormatIfNeeded(document))
									if (settings.ProtekWaybillSavingType == ProtekWaybillSavingType.SST)
										SstExporter.Save(document);
									else 
										DbfExporter.SaveProtek(document);

								scope.VoteCommit();
								WaybillOrderMatcher.ComparisonWithOrders(document, orders); // сопоставляем позиции в документе с позициями в заказе
								_logger.InfoFormat("Разобрана накладная {0} для заказа {1}", body.baseId, body.@uint);
							}
						}
					}
				}
				finally
				{
					service.closeBladingSession(new closeBladingSessionRequest(sessionId, clientId, instanceId));
					Ping(); // чтобы монитор не перезапустил рабочий поток
				}
			});
		}

		private Document ToDocument(Blading blading)
		{
			Dump(ConfigurationManager.AppSettings["DebugProtekPath"], blading);

			var orderId = (uint?) blading.@uint; // если заказы не объединены (накладной соответствует 1 заказ)
			
			IList<uint> orderIds = new List<uint>();

			if(orderId != null) orderIds.Add(orderId.Value);
			orders.Clear(); // очистка списка заказов

			if (orderId == null && blading.bladingFolder != null) // если заказы объединены (накладной соответствует несколько заказов)
			{
				orderId = blading.bladingFolder.Select(f => (uint?)f.orderUint).FirstOrDefault(id => id != null); // берем первый заказ
				orderIds = blading.bladingFolder.Where(f => f.orderUint != null).Select(f => (uint)f.orderUint.Value).Distinct().ToList(); // берем все заказы
			}

			if (orderId == null)
			{
				_logger.WarnFormat("Для накладной {0}({1}) не задан номер заказа", blading.bladingId, blading.baseId);
				return null;
			}

			var order = OrderHead.TryFind(orderId.Value);

			if (order == null)
			{
				_logger.WarnFormat("Не найден заказ {0} для накладной {1}({2})",
					orderId,
					blading.bladingId,
					blading.baseId);
				return null;
			}

			foreach (var id in orderIds)
			{
				var ord = OrderHead.TryFind(id);
				if(ord != null) orders.Add(ord);
			}

			var log = new DocumentReceiveLog {
				DocumentType = DocType.Waybill,
				ClientCode = order.ClientCode,
				Address = order.Address,
				Supplier = order.Price.Supplier,
				IsFake = true
			};

			var document = new Document(log) {
				OrderId = orderId,
				ProviderDocumentId = blading.baseId,
				DocumentDate = blading.date0,
				Parser = "ProtekHandler",
			};
			document.SetInvoice();
			var invoice = document.Invoice;
			invoice.InvoiceDate = blading.date0;
			invoice.InvoiceNumber = blading.baseId;
			invoice.SellerName = blading.protekNameAddr;
			invoice.SellerINN = blading.protekInnKpp;
			invoice.ShipperInfo = blading.protekAddr;
			invoice.ConsigneeInfo = blading.recipientAddr;
			invoice.PaymentDocumentInfo = blading.baseId;
			invoice.BuyerName = blading.payerName;
			invoice.BuyerINN = blading.payerInn;
			invoice.AmountWithoutNDS = (decimal?)blading.sumbyWonds;
			invoice.AmountWithoutNDS10 = (decimal?)blading.sumbyNdsrate10;
			invoice.NDSAmount10 = (decimal?)blading.nds10;
			invoice.AmountWithoutNDS18 = (decimal?)blading.sumbyNdsrate18;
			invoice.NDSAmount18 = (decimal?)blading.nds20;
			invoice.Amount = (decimal?)blading.rprice;

			foreach (var bladingItem in blading.bladingItems)
			{
				var line = document.NewLine();
				line.Code = bladingItem.itemId.ToString();
				line.Product = bladingItem.itemName;
				line.Producer = bladingItem.manufacturerName;
				line.Quantity = (uint?) bladingItem.bitemQty;
				line.Country = bladingItem.country;
				line.Certificates = bladingItem.seria;
				line.Period = bladingItem.prodexpiry != null ? bladingItem.prodexpiry.Value.ToShortDateString() : null;
				line.RegistryCost = (decimal?) bladingItem.reestrPrice;
				line.SupplierPriceMarkup = (decimal?) bladingItem.distrProc;
				line.NdsAmount = (decimal?) bladingItem.sumVat;
				line.Nds = (uint?) bladingItem.vat;
				line.SupplierCostWithoutNDS = (decimal?) bladingItem.distrPriceWonds;
				line.SupplierCost = (decimal?) bladingItem.distrPriceNds;
				line.ProducerCostWithoutNDS = (decimal?)bladingItem.prodPriceWonds;
				line.VitallyImportant = bladingItem.vitalMed != null ? bladingItem.vitalMed.Value == 1 : false;
				line.Amount = (decimal?)bladingItem.positionsum;
				line.SerialNumber = bladingItem.prodseria;
				line.EAN13 = bladingItem.prodsbar;
				if (bladingItem.bladingItemSeries != null)
					line.ProtekDocIds = bladingItem.bladingItemSeries
						.Where(s => s.bladingItemSeriesCertificates != null)
						.SelectMany(s => s.bladingItemSeriesCertificates)
						.Where(c => c != null)
						.Select(c => c.docId)
						.Where(id => id != null)
						.Select(id => new ProtekDoc(line, id.Value))
						.ToList();
			}

			document.SetProductId(); // сопоставляем идентификаторы названиям продуктов в накладной
			document.CalculateValues(); // расчет недостающих значений

			return document;
		}

		public static void Dump(string path, Blading blading)
		{
			if (String.IsNullOrEmpty(path))
				return;
			var file = Path.Combine(path, DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_fff") + ".xml");
			using(var stream = File.OpenWrite(file))
				blading.ToXml(stream);
		}
	}
}