using System;
using System.Runtime.Serialization;

namespace RemotePricePricessor
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

	/// <summary>
	/// интерфейс для удаленного взаимодействия с PriceProcessor'ом
	/// </summary>
	public interface IRemotePriceProcessor
	{
		/// <summary>
		/// Метод позволяющий переслать прайс-лист из истории так, как будто он был скачен
		/// </summary>
		/// <param name="downlogId">Id из таблицы logs.downlogs</param>
		void ResendPrice(ulong downlogId);

		void RetransPrice(uint priceItemId);

		string[] ErrorFiles();
		string[] InboundFiles();

		byte[] BaseFile(uint priceItemId);
	}
}
