using System;
using System.Collections.Generic;
using System.Text;

namespace RemotePricePricessor
{
	/// <summary>
	/// интерфейс для удаленного взаимодействия с PriceProcessor'ом
	/// </summary>
	public interface IRemotePricePricessor
	{
		/// <summary>
		/// Метод позволяющий переслать прайс-лист из истории так, как будто он был скачен
		/// </summary>
		/// <param name="DownLogId">Id из таблицы logs.downlogs</param>
		void ResendPrice(ulong DownLogId);
	}
}
