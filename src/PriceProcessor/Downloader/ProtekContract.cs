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
		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string NPost { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string baseId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? bladingId { get; set; }

		[XmlElement("bladingItems", Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public BladingItem[] bladingItems { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? bladingSid { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? bladingType { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string bladingTypeName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? buyerId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string buyerName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? clientOrderId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string currency { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? date0 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? dated { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? dbd { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? dkd { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? ksMin { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? moddate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string moddateAsString { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string nPost { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string ncontr2 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? nds10 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? nds20 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? orderDate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? orderId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? orgId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? paydate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string payerAddr { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string payerEmail { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? payerId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string payerInn { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string payerName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? poscount { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? rate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string recipientAddr { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? recipientId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string recipientInn { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string recipientName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? rprice { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? sellerId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string sellerInn { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string sellerName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? shipdate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? sumbyNdsrate10 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? sumbyNdsrate18 { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? sumbyWonds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string tenderLotNumber { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string tenderNumber { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? udat { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? udec { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? @uint { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
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