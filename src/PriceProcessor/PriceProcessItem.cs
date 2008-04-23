using System;
using System.Collections.Generic;
using System.Text;

namespace Inforoom.PriceProcessor
{
	public class PriceProcessItem
	{
		private bool _downloaded;
		private string _filePath;
		private ulong _priceCode;
		private ulong? _costCode;
		private ulong _priceItemId;
		private DateTime? _fileTime;
		private DateTime _createTime;

		//Скачан ли прайс-лист или переподложен
		public bool Downloaded
		{
			get { return _downloaded; }
		}

		//путь к прайс-листу: либо папа Inbound на fms или временная папка для скаченных прайсов
		public string FilePath
		{
			get { return _filePath; }
			set { _filePath = value; }
		}

		//код прайс-листа
		public ulong PriceCode
		{
			get { return _priceCode; }
		}

		//код цены, может быть null
		public ulong? CostCode
		{
			get { return _costCode; }
		}

		//Id из таблицы PriceItems
		public ulong PriceItemId
		{
			get { return _priceItemId; }
		}

		//Дата файла, взятая из сети, ftp или http, чтобы определить: необходимо ли скачивать еще раз данный прайс?
		public DateTime? FileTime
		{
			get { return _fileTime; }
			set { _fileTime = value; }
		}

		//Дата создания элемента, чтобы знать: можно ли его брать в обработку или нет
		public DateTime CreateTime
		{
			get { return _createTime; }
		}

		public PriceProcessItem(bool downloaded, ulong priceCode, ulong? costCode, ulong priceItemId, string filePath)
		{
			_downloaded = downloaded;
			_priceCode = priceCode;
			_costCode = costCode;
			_priceItemId = priceItemId;
			_filePath = filePath;
			_createTime = DateTime.UtcNow;
		}
	}
}
