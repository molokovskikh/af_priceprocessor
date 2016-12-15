﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace RemotePriceProcessor
{
	public class AuditInfoInspector : IClientMessageInspector
	{
		public object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			request.Headers.Add(MessageHeader.CreateHeader("UserName", "", Environment.UserName));
			request.Headers.Add(MessageHeader.CreateHeader("MachineName", "", Environment.MachineName));
			return null;
		}

		public void AfterReceiveReply(ref Message reply, object correlationState)
		{
		}
	}

	public class MessageInspectorRegistrator : IEndpointBehavior
	{
		private readonly IClientMessageInspector[] _inspectors;

		public MessageInspectorRegistrator(IClientMessageInspector[] inspectors)
		{
			_inspectors = inspectors;
		}

		public void Validate(ServiceEndpoint endpoint)
		{
		}

		public void AddBindingParameters(ServiceEndpoint endpoint,
			BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			foreach (var inspector in _inspectors)
				clientRuntime.MessageInspectors.Add(inspector);
		}
	}

	[Serializable]
	public class PriceProcessorException : ApplicationException
	{
		public PriceProcessorException()
		{
		}

		public PriceProcessorException(string message) : base(message)
		{
		}

		public PriceProcessorException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected PriceProcessorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}

	[MessageContract]
	public class WcfCallParameter
	{
		[MessageHeader]
		public object Value { get; set; }

		[MessageHeader]
		public LogInformation LogInformation { get; set; }
	}

	/// <summary>
	/// Класс, содержащий данные для логирования действий пользователя
	/// </summary>
	public class LogInformation
	{
		public string ComputerName { get; set; }

		public string UserName { get; set; }
	}

	[MessageContract]
	public class HistoryFile
	{
		[MessageHeader]
		public string Filename { get; set; }

		[MessageBodyMember]
		public Stream FileStream { get; set; }
	}

	[DataContract]
	public class WcfPriceProcessItem
	{
		public WcfPriceProcessItem()
		{
		}

		public WcfPriceProcessItem(ulong priceCode, bool downloaded, string filePath, ulong priceItemId, DateTime? fileTime, DateTime createTime, int hashCode)
		{
			PriceCode = priceCode;
			Downloaded = downloaded;
			FilePath = filePath;
			PriceItemId = priceItemId;
			FileTime = fileTime;
			CreateTime = createTime;
			HashCode = hashCode;
		}

		[DataMember]
		public ulong PriceCode { get; set; }

		[DataMember]
		public bool Downloaded { get; set; }

		[DataMember]
		public string FilePath { get; set; }

		[DataMember]
		public ulong PriceItemId { get; set; }

		[DataMember]
		public DateTime? FileTime { get; set; }

		[DataMember]
		public DateTime CreateTime { get; set; }

		[DataMember]
		public int HashCode { get; set; }

		[DataMember]
		public bool FormalizedNow { get; set; }
	}

	[MessageContract]
	public class FilePriceInfo : IDisposable
	{
		[MessageHeader] public uint PriceItemId;

		[MessageHeader] public LogInformation LogInformation;

		[MessageBodyMember] public Stream Stream;

		public void Dispose()
		{
			if (Stream != null) {
				Stream.Close();
			}
		}
	}


	[DataContract]
	public enum TaskState
	{
		[EnumMember]
		None,
		[EnumMember]
		Running,
		[EnumMember]
		Success,
		[EnumMember]
		Error,
		[EnumMember]
		Canceled
	}


	/// <summary>
	/// интерфейс для удаленного взаимодействия через MSMQ
	/// </summary>
	[ServiceContract]
	public interface IRemotePriceProcessorOneWay
	{
		[OperationContract(IsOneWay = true)]
		void RetransPrice(WcfCallParameter downlogId);

		[OperationContract(IsOneWay = true)]
		void RetransPriceSmart(uint priceId);
	}


	[DataContract]
	public class WcfSynonymItem
	{
		[DataMember]
		public uint FirmCode { get; set; }

		[DataMember]
		public string FirmName { get; set; }

		[DataMember]
		public uint ProductId { get; set; }

		[DataMember]
		public bool Junk { get; set; }
	}

	[DataContract]
	public class WcfSynonymList
	{
		public WcfSynonymList()
		{
			SynonymList = new List<WcfSynonymItem>();
		}

		[DataMember]
		public string OriginalName { get; set; }

		[DataMember]
		public List<WcfSynonymItem> SynonymList { get; set; }
	}

	[DataContract]
	public class WcfSynonymBox
	{
		public WcfSynonymBox()
		{
			SynonymBox = new List<WcfSynonymList>();
		}

		[DataMember]
		public TaskState Status { get; set; }

		[DataMember]
		public string Message { get; set; }

		[DataMember]
		public List<WcfSynonymList> SynonymBox { get; set; }
	}

	/// <summary>
	/// интерфейс для удаленного взаимодействия с PriceProcessor'ом
	/// </summary>
	[ServiceContract]
	public interface IRemotePriceProcessor
	{
		/// <summary>
		/// Метод позволяющий переслать прайс-лист из истории так, как будто он был скачен
		/// </summary>
		/// <param name="downlogId">Id из таблицы logs.downlogs</param>
		[OperationContract]
		void ResendPrice(WcfCallParameter paramDownlogId);

		[OperationContract]
		void RetransPrice(WcfCallParameter downlogId);

		[OperationContract]
		void RetransPriceSmart(uint priceId);

		[OperationContract]
		string[] ErrorFiles();

		[OperationContract]
		string[] InboundFiles();

		[OperationContract]
		WcfPriceProcessItem[] GetPriceItemList();

		[OperationContract]
		bool TopInInboundList(int hashCode);

		[OperationContract]
		bool DeleteItemInInboundList(int hashCode);

		[OperationContract]
		Stream BaseFile(uint priceItemId);

		[OperationContract]
		HistoryFile GetFileFormHistory(WcfCallParameter downlogId);

		[OperationContract]
		void PutFileToInbound(FilePriceInfo filePriceInfo);

		[OperationContract]
		void PutFileToBase(FilePriceInfo filePriceInfo);

		[OperationContract]
		void RetransErrorPrice(WcfCallParameter priceItemId);

		[OperationContract]
		string[] FindSynonyms(uint priceItemId);

		[OperationContract]
		WcfSynonymBox FindSynonymsResult(string taskId);

		[OperationContract]
		void StopFindSynonyms(string taskId);

		[OperationContract]
		void AppendToIndex(string[] synonymsId);
	}
}