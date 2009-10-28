﻿using System;
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

	[Serializable]
	public class HistoryFile
	{
		public string Filename { get; set; }
		public Stream FileStream { get; set; }
	}

	[MessageContract]
	public class FilePriceInfo : IDisposable
	{
		[MessageHeader]
		public uint PriceItemId;

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
		void ResendPrice(ulong downlogId);

		[OperationContract]
		void RetransPrice(uint priceItemId);

		[OperationContract]
		string[] ErrorFiles();

		[OperationContract]
		string[] InboundFiles();

		[OperationContract]
		Stream BaseFile(uint priceItemId);

		[OperationContract]
		HistoryFile GetFileFormHistory(ulong downlogId);

		[OperationContract]
		void PutFileToBase(FilePriceInfo filePriceInfo);
	}
}
