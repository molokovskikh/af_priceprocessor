using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace RemotePriceProcessor
{
	[Serializable]
	public class PriceProcessorException : ApplicationException
	{
		public PriceProcessorException()
		{}

		public PriceProcessorException(string message) : base(message)
		{}

		public PriceProcessorException(string message, Exception innerException) : base(message, innerException)
		{}

		protected PriceProcessorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{}
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

	[MessageContract]
	public class FilePriceInfo : IDisposable
	{
		[MessageHeader]
		public uint PriceItemId;

		[MessageHeader]
		public LogInformation LogInformation;

		[MessageBodyMember]
		public Stream Stream;

		public void Dispose()
		{
			if (Stream != null)
			{
				Stream.Close();
			}
		}
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
		string[] ErrorFiles();

		[OperationContract]
		string[] InboundFiles();

		[OperationContract]
		string[] InboundPriceItemIds();

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
	}
}
