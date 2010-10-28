using System;
using System.ServiceModel;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Inforoom.PriceProcessor.Downloader
{
	[SerializableAttribute]
	[XmlTypeAttribute(Namespace="http://domain.ezakaz.protek.ru/xsd")]
	public class BladingItem
	{
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? itemId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int bitemQty { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string country { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double distrPriceNds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double distrPriceWonds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? distrProc { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string itemName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string manufacturerName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? prodPriceWnds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? prodPriceWonds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public DateTime? prodexpiry { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string prodseria { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? reestrPrice { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? vat { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? vitalMed { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string seria { get; set; }
	}

	[SerializableAttribute]
	[XmlTypeAttribute(Namespace = "http://domain.ezakaz.protek.ru/xsd")]
	public class Blading
	{
		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 0)]
		public string NPost { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 1)]
		public string baseId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 3)]
		public int bladingId { get; set; }

		[XmlElement("bladingItems", Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 4)]
		public BladingItem[] bladingItems { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 5)]
		public int bladingSid { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 6)]
		public int? bladingType { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 7)]
		public string bladingTypeName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 8)]
		public int? buyerId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 9)]
		public string buyerName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 10)]
		public int? clientOrderId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 11)]
		public string currency { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 12)]
		public DateTime? date0 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 13)]
		public DateTime? dated { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 14)]
		public int? dbd { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 15)]
		public int? dkd { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 16)]
		public double? ksMin { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 17)]
		public DateTime? moddate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 18)]
		public string moddateAsString { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 19)]
		public string nPost { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 20)]
		public string ncontr2 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 21)]
		public double? nds10 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 22)]
		public double? nds20 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 23)]
		public DateTime? orderDate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 24)]
		public int? orderId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 25)]
		public int? orgId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 26)]
		public DateTime? paydate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 27)]
		public string payerAddr { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 28)]
		public string payerEmail { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 29)]
		public int? payerId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 30)]
		public string payerInn { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 31)]
		public string payerName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 32)]
		public int? poscount { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 33)]
		public double? rate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 34)]
		public string recipientAddr { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 35)]
		public int? recipientId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 36)]
		public string recipientInn { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 37)]
		public string recipientName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 38)]
		public double? rprice { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 39)]
		public int? sellerId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 40)]
		public string sellerInn { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 41)]
		public string sellerName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 42)]
		public DateTime? shipdate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 43)]
		public double? sumbyNdsrate10 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 44)]
		public double? sumbyNdsrate18 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 45)]
		public double? sumbyWonds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 46)]
		public string tenderLotNumber { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 47)]
		public string tenderNumber { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 48)]
		public DateTime? udat { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 49)]
		public double? udec { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 50)]
		public int? @uint { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true, Order = 51)]
		public string ustr { get; set; }
	}

	[SerializableAttribute]
	[XmlTypeAttribute(Namespace="http://xml.ezakaz.protek.ru/xsd")]
	public class EZakazXML 
	{
		[XmlElementAttribute("blading", Form = XmlSchemaForm.Unqualified)]
		public Blading[] blading { get; set; }

		[XmlElementAttribute(Form = XmlSchemaForm.Unqualified)]
		public string wsSessionIdStr { get; set; }
	}

	[SerializableAttribute]
	[MessageContract(WrapperName="getBladingHeaders", WrapperNamespace="http://service.ezakaz.protek.ru", IsWrapped=true)]
	public class getBladingHeadersRequest {
		
		[MessageBodyMember(Namespace="http://service.ezakaz.protek.ru", Order=0)]
		[XmlElement(Form=XmlSchemaForm.Unqualified)]
		public int clientId;
		
		[MessageBodyMemberAttribute(Namespace="http://service.ezakaz.protek.ru", Order=1)]
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public int instCode;
		
		public getBladingHeadersRequest() { }
		
		public getBladingHeadersRequest(int clientId, int instCode) {
			this.clientId = clientId;
			this.instCode = instCode;
		}
	}

	[SerializableAttribute]
	[MessageContract(WrapperName = "getBladingBody", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class getBladingBodyRequest
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 0)] [XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)] public string theUid;

		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 1)]
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int clientId;

		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 2)]
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int instCode;

		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 3)]
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int bladingId;

		public getBladingBodyRequest()
		{
		}

		public getBladingBodyRequest(string theUid, int clientId, int instCode, int bladingId)
		{
			this.theUid = theUid;
			this.clientId = clientId;
			this.instCode = instCode;
			this.bladingId = bladingId;
		}
	}

	[SerializableAttribute]
	[MessageContract(WrapperName = "getBladingHeadersResponse", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class getBladingHeadersResponse
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 0)]
		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public EZakazXML @return;

		public getBladingHeadersResponse()
		{
		}

		public getBladingHeadersResponse(EZakazXML @return)
		{
			this.@return = @return;
		}
	}

	[SerializableAttribute]
	[MessageContract(WrapperName = "getBladingBodyResponse", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class getBladingBodyResponse
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 0)]
		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public EZakazXML @return;

		public getBladingBodyResponse()
		{
		}

		public getBladingBodyResponse(EZakazXML @return)
		{
			this.@return = @return;
		}
	}

	[SerializableAttribute]
	[MessageContract(WrapperName = "closeBladingSession", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class closeBladingSessionRequest
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 0)] [XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)] public string theUid;

		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 1)] [XmlElement(Form = XmlSchemaForm.Unqualified)] public int clientId;

		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 2)] [XmlElement(Form = XmlSchemaForm.Unqualified)] public int instCode;

		public closeBladingSessionRequest()
		{
		}

		public closeBladingSessionRequest(string theUid, int clientId, int instCode)
		{
			this.theUid = theUid;
			this.clientId = clientId;
			this.instCode = instCode;
		}
	}

	[MessageContractAttribute(WrapperName="closeBladingSessionResponse", WrapperNamespace="http://service.ezakaz.protek.ru", IsWrapped=true)]
	public class closeBladingSessionResponse {

		[MessageBodyMemberAttribute(Namespace="http://service.ezakaz.protek.ru", Order=0)]
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public bool @return;

		public closeBladingSessionResponse() {
		}

		public closeBladingSessionResponse(bool @return) {
			this.@return = @return;
		}
	}

}