using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
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
		public string prodseria { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string prodsbar { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public DateTime? prodexpiry { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? expiry { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public DateTime? proddt { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? reestrPrice { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public DateTime? reestrDate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? vat { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? sumVat { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? vitalMed { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string seria { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string unit { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? positionsum { get; set; }

		[XmlElement("bladingItemSeries", Form=XmlSchemaForm.Unqualified)]
		public BladingItemSeries[] bladingItemSeries { get; set; }
	}

	[Serializable]
	[XmlType(Namespace = "http://domain.ezakaz.protek.ru/xsd")]
	public class SertImagesBase
	{
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? docId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, DataType = "base64Binary")]
		public byte[] image { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? imageSize { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? leafNumber { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? saveTypeId {get; set; }
	}

	[Serializable]
	[XmlType(Namespace="http://domain.ezakaz.protek.ru/xsd")]
	public class BladingItemSeries
	{
		[XmlElement(Form=XmlSchemaForm.Unqualified)]
		public int? bitemId { get;set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? bitemSeriesId { get; set; }

		[XmlElement("bladingItemSeriesCertificates", Form = XmlSchemaForm.Unqualified)]
		public BladingItemSeriesCertificate[] bladingItemSeriesCertificates { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string country { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string countryCode { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public DateTime? dateExpire { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public DateTime? dateProduce { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string descBrak { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? grcPrice { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string gtdn { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? impProc { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? itemId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? lotsSumWnds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? optProc { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? prodPrice { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public double? prodPriceWnds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? qty { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public float? rrpriceNonds { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? sbad { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string sbar { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string seria { get; set; }
	}

	[Serializable]
	[XmlType(Namespace="http://domain.ezakaz.protek.ru/xsd")]
	public class BladingItemSeriesCertificate
	{
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public int? bitemSeriesId { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public DateTime? dateExpire { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public string declarantName { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public int? docId { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public int? docTypeId { get; set; }

		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public int? itemId { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public string regNo { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public string regOrg { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public string regSert { get; set; }
		
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public System.DateTime? regd { get; set; }

		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified)]
		public int? sertId { get; set; }
	}

	[SerializableAttribute]
	[XmlTypeAttribute(Namespace="http://domain.ezakaz.protek.ru/xsd")]
	public class BladingFolder
	{
		[XmlElementAttribute(Form=XmlSchemaForm.Unqualified, IsNullable=true)]
		public int? bladingId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string folderNum { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? orderDate { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? orderId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string orderNum { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public DateTime? orderUdat { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public double? orderUdec { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? orderUint { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string orderUstr { get; set; }
	}

	[SerializableAttribute]
	[XmlTypeAttribute(Namespace = "http://domain.ezakaz.protek.ru/xsd")]
	public class Blading
	{
		[XmlElement(Form=XmlSchemaForm.Unqualified, IsNullable=true)]
		public BladingFolder[] bladingFolder { get; set; }

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

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string protekAddr { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string protekNameAddr { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string protekInnKpp { get; set; }
	}

	[Serializable]
	[XmlType(Namespace="http://xml.ezakaz.protek.ru/xsd")]
	public class EZakazXML 
	{
		[XmlElement("blading", Form = XmlSchemaForm.Unqualified)]
		public Blading[] blading { get; set; }

		[XmlElement("sertImage", Form = XmlSchemaForm.Unqualified)]
		public SertImagesBase[] sertImage { get; set; }

		[XmlElement("sertDocType", Form=XmlSchemaForm.Unqualified)]
		public SertDocType[] sertDocType { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string wsSessionIdStr { get; set; }
	}

	[Serializable]
	[XmlType(Namespace = "http://domain.ezakaz.protek.ru/xsd")]
	public class SertDocType
	{
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string description { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? docTypeId { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public string docTypeName { get; set; }

		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int? organizationId { get; set; }
	}

	[SerializableAttribute]
	[MessageContract(WrapperName = "getBladingHeaders", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class getBladingHeadersRequest
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 0)]
		[XmlElement(Form = XmlSchemaForm.Unqualified)]
		public int clientId;

		[MessageBodyMemberAttribute(Namespace = "http://service.ezakaz.protek.ru", Order = 1)]
		[XmlElementAttribute(Form = XmlSchemaForm.Unqualified)]
		public int instCode;

		public getBladingHeadersRequest()
		{}

		public getBladingHeadersRequest(int clientId, int instCode)
		{
			this.clientId = clientId;
			this.instCode = instCode;
		}
	}

	[SerializableAttribute]
	[MessageContract(WrapperName = "getBladingBody", WrapperNamespace = "http://service.ezakaz.protek.ru", IsWrapped = true)]
	public class getBladingBodyRequest
	{
		[MessageBodyMember(Namespace = "http://service.ezakaz.protek.ru", Order = 0)]
		[XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string theUid;

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
		{}

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