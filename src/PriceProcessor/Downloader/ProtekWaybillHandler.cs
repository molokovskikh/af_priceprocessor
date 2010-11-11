﻿using System;
using System.ServiceModel;
using Castle.ActiveRecord;
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
	public class Order : ActiveRecordBase<Order>
	{
		[PrimaryKey("RowId")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual uint? AddressId { get; set; }

		[Property]
		public virtual uint ClientCode { get; set; }

		[BelongsTo("PriceCode")]
		public virtual Price Price { get; set; }
	}

	public class ProtekWaybillHandler : AbstractHandler
	{
		private string uri;
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
			catch (Exception)
			{
				if (communicationObject.State != CommunicationState.Closed)
					communicationObject.Abort();
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
		}

		private void Load(int clientId, int instanceId)
		{
			WithService(service => {
				var responce = service.getBladingHeaders(new getBladingHeadersRequest(clientId, instanceId));
				var sessionId = responce.@return.wsSessionIdStr;
				_logger.InfoFormat("Получили накладные, всего {0} для сесии {1}", responce.@return.blading.Length, sessionId);
				try
				{
					foreach (var blading in responce.@return.blading)
					{
						var blanding = service.getBladingBody(new getBladingBodyRequest(sessionId, clientId, instanceId, blading.bladingId.Value));
						_logger.InfoFormat("Загрузил накладную {0}", blading.bladingId.Value);
						foreach (var body in blanding.@return.blading)
						{
							using (var scope = new TransactionScope(OnDispose.Rollback))
							{
								var document = ToDocument(body);
								document.Log.Save();
								document.Save();
								scope.VoteCommit();
								_logger.InfoFormat("Разобрана накладная {0} для заказа {1}", body.baseId, body.clientOrderId);
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

		private Document ToDocument(Blading blading)
		{
			var order = Order.Find((uint) blading.clientOrderId.Value);

			var log = new DocumentReceiveLog {
				DocumentType = DocType.Waybill,
				FileName = "fake.txt",
				ClientCode = order.ClientCode,
				AddressId = order.AddressId,
				Supplier = order.Price.Supplier,
				IsFake = true,
			};

			var document = new Document(log) {
				OrderId = (uint?) blading.clientOrderId,
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
				line.Period = bladingItem.prodexpiry.Value.ToShortDateString();
				line.RegistryCost = (decimal?) bladingItem.reestrPrice;
				line.SupplierPriceMarkup = (decimal?) bladingItem.distrProc;
				line.Nds = (uint?) bladingItem.vat;
				line.SupplierCostWithoutNDS = (decimal?) bladingItem.distrPriceWonds;
				line.SupplierCost = (decimal?) bladingItem.distrPriceNds;
				line.ProducerCost = (decimal?) bladingItem.prodPriceWonds;
				line.VitallyImportant = bladingItem.vitalMed.Value == 1;
				line.SerialNumber = bladingItem.prodseria;
			}
			return document;
		}
	}
}