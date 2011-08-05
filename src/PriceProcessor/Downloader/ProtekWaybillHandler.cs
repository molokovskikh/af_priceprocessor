using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Waybills;

namespace Inforoom.PriceProcessor.Downloader
{
	[ServiceContractAttribute(Namespace="http://service.ezakaz.protek.ru")]
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
	}

	[ActiveRecord("OrdersHead", Schema = "Orders")]
	public class OrderHead : ActiveRecordLinqBase<OrderHead>
	{
		[PrimaryKey("RowId")]
		public virtual uint Id { get; set; }

        [Property]
        public DateTime WriteTime { get; set; }

		[Property]
		public virtual uint? AddressId { get; set; }

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


	public class ProtekWaybillHandler : AbstractHandler
	{
		private string uri;
        private IList<OrderHead> orders = new List<OrderHead>();

		public virtual void WithService(Action<ProtekService> action)
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
			//калуга
			uri = "http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(79888, 1024847);

			//воронеж
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(123108, 1064974);

			//Курск/Белгород
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(118855, 1053374);

			//тамбов
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(261544, 1072996);

			//Москва
			uri = "http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(83674, 1033812);

			//Смоленск
			uri = "http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(83868, 1033815);

			//Казань
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(231691, 1072909);

			//Екатеринбург
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(261542, 1072994);

			//Пермь
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(215115, 1072912);

			//Челябинск
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(261543, 1072995);

			//Киров
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(215116, 1072913);

			//Орел
			uri = "http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(83472, 1033813);

			//Тюмень
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(250434, 1072911);

			//ХМАО-Югра
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(245011, 1072914);

			//Омск
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(265488, 1077902);

			//Протек-02 Волгоград
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(266327, 1079618);

			//Протек-12 Самара
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(266329, 1079620);

			//Протек-16 Новосибирск
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(266337, 1079622);

			//Протек-05 Нижний Новгород
			uri = "http://wezakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(86980, 1036488);

			//Протек-36 Оренбург
			uri = "http://wjzakaz.protek.ru:20080/axis2/services/EzakazWebService.EzakazWebServiceHttpSoap12Endpoint/";
			Load(266508, 1080034);
		}

		protected void Load(int clientId, int instanceId)
		{
			WithService(service => {
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
								DocumentReceiveLog log = null;
								var document = ToDocument(body, ref log);
								if (document == null)
									continue;
								document.Log.Save();
								document.Save();
								scope.VoteCommit();
                                WaybillService.ComparisonWithOrders(document, orders); // сопоставляем позиции в документе с позициями в заказе
								_logger.InfoFormat("Разобрана накладная {0} для заказа {1}", body.baseId, body.@uint);
							}
                            
						}
					}
				}
				finally
				{
					service.closeBladingSession(new closeBladingSessionRequest(sessionId, clientId, instanceId));
				}
			});
		}

		private Document ToDocument(Blading blading, ref DocumentReceiveLog log)
		{
			var orderId = (uint?) blading.@uint; // если заказы не объединены (накладной соответствует 1 заказ)
		    
            IList<uint> orderIds = new List<uint>();

            if(orderId != null) orderIds.Add(orderId.Value);

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

		    string ordstr = String.Empty;
		    foreach (var ord in orders)
		    {
		        ordstr += ord.Id + " ";
		    }
		    _logger.InfoFormat("Заказы в накладной Протека: {0}", ordstr);

			log = new DocumentReceiveLog 
			{
				DocumentType = DocType.Waybill,
				FileName = "fake.txt",
				ClientCode = order.ClientCode,
				AddressId = order.AddressId,
				Supplier = order.Price.Supplier,
				IsFake = true,
			};

			var document = new Document(log) {
				OrderId = orderId,
				ProviderDocumentId = blading.baseId,
				DocumentDate = blading.date0,
				Parser = "ProtekHandler"
			};           

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
				line.ProducerCost = (decimal?) bladingItem.prodPriceWonds;
				line.VitallyImportant = bladingItem.vitalMed != null ? bladingItem.vitalMed.Value == 1 : false;
				line.SerialNumber = bladingItem.prodseria;
				line.SetValues();
			}
		    
			document.SetProductId(); // сопоставляем идентификаторы названиям продуктов в накладной            
			var settings = WaybillSettings.TryFind(order.ClientCode);
			if (settings != null && settings.IsConvertFormat)			
				WaybillService.ConvertAndSaveDbfFormatIfNeeded(document, log, true);

			return document;
		}
	}
}