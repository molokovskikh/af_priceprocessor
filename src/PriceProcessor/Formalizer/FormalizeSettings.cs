using System;
using System.Configuration;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for FormalizeSettings.
	/// </summary>
	public sealed class FormalizeSettings
	{
		//Код приложения для логирования
		public static readonly int AppCode = 7;
		//Максимальное время жизни нитки
		public static readonly int MaxLiveTime = 10;

		public static readonly bool CheckZero = false;

		//Сегмент рынка
		public static readonly int Segment = 0;
		//Разновалютный прайс
		public static readonly string DIFF_CURR = "разн.";
		//Флаг ассортиментного прайса
		public static readonly int ASSORT_FLG = 1;

		//Пометка того, что позиция просрочена
		public static readonly string JUNK		= "срок";	
		//Пометка того, что позиция "ожидается"
		public static readonly string AWAIT		= "ожид";	

		//Форматы файлов
		// Таблица Excel
		public static readonly string EXCEL_FMT = "XLS";
		// Таблица dBase
		public static readonly string DBF_FMT = "DBF";
		// Таблица Paradox
		public static readonly string DB_FMT = "DB";
		// Текстовый файл в Win кодировке
		public static readonly string WIN_FMT = "WIN";
		// Текстовый файл в DOS кодировке
		public static readonly string DOS_FMT = "DOS";
		// XML-файл в формате CommerceML
		public static readonly string XML_FMT = "XML";	


		public static readonly string DateMask = @"([^\d]*(?<Day>\d*?)[\s\.\-\/])?(?<Month>[^\s\.\-\/]*?)[\s\.\-\/](?<Year>\d+)([^\d].*)?$";

		//Путь с входящими прайсами
		public static readonly string InboundPath = "C:\\Temp\\Inbound\\";
		public static readonly string BasePath = "C:\\Temp\\Base\\";
		public static readonly string InboundCopy = "C:\\Temp\\InboundCopy\\";

		//Каталог с ошибочными файлами
		public static readonly string ErrorFiles = "C:\\Temp\\ErrorFiles\\";
		//Кол-во неудачных попыток формализации прайса, в результате которых он помещается в ErrorFiles
		public static readonly int MaxErrorCount = 3;

		//Максимально кол-во одновременно работающих нитей
		public static readonly int MaxWorkThread = 4;

		//Максимально кол-во повторов запроса при блокировки
		public static readonly int MaxRepeatTranCount = 15;
		//Допустимое кол-во повторов запроса при блокировки
		public static readonly int MinRepeatTranCount = 7;

		//database settings
		public static readonly string ServerName = "sql.analit.net";
		public static readonly string UserName = "mor";
		public static readonly string Pass = "vozorom";
		public static readonly string DatabaseName = "temp";

		//table names
		public static readonly string tbFormLogs = "temp.FormLogs";
		public static readonly string tbFormRules = "temp.FormRules";
		public static readonly string tbPricesCosts = "temp.PricesCosts";
		public static readonly string tbForbidden = "farm.Forbidden";
		public static readonly string tbSynonym = "farm.Synonym";		
		public static readonly string tbSynonymFirmCr = "farm.SynonymFirmCr";
		public static readonly string tbSynonymCurrency = "farm.SynonymCurrency";
		public static readonly string tbCore = "temp.Core0";
		public static readonly string tbUnrecExp = "temp.UnrecExp";
		public static readonly string tbZero = "temp.Zero";
		public static readonly string tbForb = "temp.Forb";
		public static readonly string tbCoreCosts = "temp.CoreCosts";
		public static readonly string tbCostsFormRules = "temp.CostsFormRules";
		public static readonly string tbBlockedPrice = "farm.blockedprice";

		public static readonly string RepEmail = "morozov@analit.net";
		public static readonly string FromEmail = "WarningList@subscribe.analit.net";


		public static readonly string UnknownPriceError = "Не возможно сопоставить прайс файлу : ({0})";
		public static readonly string DisableByBillingStatusError = "Прайс отключен по причине : BillingStatus";
		public static readonly string DisableByFirmStatusError = "Прайс отключен по причине : FirmStatus";
		public static readonly string UnknownPriceFMTError = "Неизвестный формат прайса : ({0})";
		public static readonly string SheetsNotExistsError = "Файл не содержит листов";
		public static readonly string CostsNotExistsError = "Прайс не является ассортиментным, но не содержит ни одной цены.";
		public static readonly string BaseCostNotExistsError = "Прайс не имеет базовой цены.";
		public static readonly string DoubleBaseCostsError = "Прайс имеет две базовые цены : {0} и {1}.";
		public static readonly string FieldNameBaseCostsError = "У базовой цены прайса не определенно название поля.";
		public static readonly string ZeroRollbackError = "Кол-во нулевых позиций выше допустимого.";
		public static readonly string PrevFormRollbackError = "Кол-во формализованных позиций меньше кол-ва формализованных в прошлый раз.";
		public static readonly string FieldsNotExistsError = "Лист не содержит полей.";
		public static readonly string ParseMaskError = "Не удалось разобрать маску имени {0}";
		public static readonly string DoubleGroupMaskError = "В маски имени два раза встречается группа \"{0}\".";
		public static readonly string MinFieldCountError = "Кол-во полей, объявленных для распознания, меньше 1.";
		public static readonly string PeriodParseError = "Ошибка в разборе срока годности для значения '{0}'.";
		public static readonly string ThreadAbortError = "Формализация прайса был остановленна.";
		public static readonly string FileCopyError = "Не удалось скопировать файл {0} в каталог {1}. \nПричина : {2}";
		public static readonly string MaxErrorsError = "Файл {0} помещен в каталог {1} в результате {2} неудачных попыток формализации.";
		public static readonly string UnknowCostTypeError = "Неизвестный тип ценовых колонок прайса : ({0})";
		public static readonly string MulticolumnAsPriceError = "Попытка формализовать ценовую колонку мультиколоночного прайс-листа как прайс.";


		static FormalizeSettings()
		{
			AppSettingsReader configurationAppSettings = new AppSettingsReader();			
			Segment = (int)configurationAppSettings.GetValue("FormalizeSettings.Segment", typeof(int));
			AppCode = (int)configurationAppSettings.GetValue("FormalizeSettings.AppCode", typeof(int));
			MaxLiveTime = (int)configurationAppSettings.GetValue("FormalizeSettings.MaxLiveTime", typeof(int));
			CheckZero = (bool)configurationAppSettings.GetValue("FormalizeSettings.CheckZero", typeof(bool));
			InboundPath = (string)configurationAppSettings.GetValue("FormalizeSettings.InboundPath", typeof(string));
			BasePath = (string)configurationAppSettings.GetValue("FormalizeSettings.BasePath", typeof(string));
			InboundCopy = (string)configurationAppSettings.GetValue("FormalizeSettings.InboundCopy", typeof(string));
			MaxWorkThread = (int)configurationAppSettings.GetValue("FormalizeSettings.MaxWorkThread", typeof(int));
			MaxRepeatTranCount = (int)configurationAppSettings.GetValue("FormalizeSettings.MaxRepeatTranCount", typeof(int));
			MinRepeatTranCount = (int)configurationAppSettings.GetValue("FormalizeSettings.MinRepeatTranCount", typeof(int));

			ErrorFiles = (string)configurationAppSettings.GetValue("FormalizeSettings.ErrorFiles", typeof(string));
			MaxErrorCount = (int)configurationAppSettings.GetValue("FormalizeSettings.MaxErrorCount", typeof(int));

			DateMask = (string)configurationAppSettings.GetValue("FormalizeSettings.DateMask", typeof(string));

			RepEmail = (string)configurationAppSettings.GetValue("FormalizeSettings.RepEmail", typeof(string));
			FromEmail = (string)configurationAppSettings.GetValue("FormalizeSettings.FromEmail", typeof(string));

			ServerName = (string)configurationAppSettings.GetValue("FormalizeSettings.ServerName", typeof(string));
			UserName = (string)configurationAppSettings.GetValue("FormalizeSettings.UserName", typeof(string));
			Pass = (string)configurationAppSettings.GetValue("FormalizeSettings.Pass", typeof(string));
			DatabaseName = (string)configurationAppSettings.GetValue("FormalizeSettings.DatabaseName", typeof(string));

			tbFormLogs = (string)configurationAppSettings.GetValue("FormalizeSettings.FormLogs", typeof(string));
			tbFormRules = (string)configurationAppSettings.GetValue("FormalizeSettings.FormRules", typeof(string));
			tbPricesCosts = (string)configurationAppSettings.GetValue("FormalizeSettings.PricesCosts", typeof(string));
			tbForbidden = (string)configurationAppSettings.GetValue("FormalizeSettings.Forbidden", typeof(string));			
			tbSynonym = (string)configurationAppSettings.GetValue("FormalizeSettings.Synonym", typeof(string));
			tbSynonymFirmCr = (string)configurationAppSettings.GetValue("FormalizeSettings.SynonymFirmCr", typeof(string));
			tbSynonymCurrency = (string)configurationAppSettings.GetValue("FormalizeSettings.SynonymCurrency", typeof(string));
			tbCore = (string)configurationAppSettings.GetValue("FormalizeSettings.Core", typeof(string));
			tbUnrecExp = (string)configurationAppSettings.GetValue("FormalizeSettings.UnrecExp", typeof(string));
			tbZero = (string)configurationAppSettings.GetValue("FormalizeSettings.Zero", typeof(string));
			tbForb = (string)configurationAppSettings.GetValue("FormalizeSettings.Forb", typeof(string));
			tbCoreCosts = (string)configurationAppSettings.GetValue("FormalizeSettings.CoreCosts", typeof(string));
			tbCostsFormRules = (string)configurationAppSettings.GetValue("FormalizeSettings.CostsFormRules", typeof(string));
			tbBlockedPrice = (string)configurationAppSettings.GetValue("FormalizeSettings.BlockedPrice", typeof(string));

			UnknownPriceError = (string)configurationAppSettings.GetValue("FormalizeSettings.UnknownPriceError", typeof(string));
			DisableByBillingStatusError = (string)configurationAppSettings.GetValue("FormalizeSettings.DisableByBillingStatusError", typeof(string));
			DisableByFirmStatusError = (string)configurationAppSettings.GetValue("FormalizeSettings.DisableByFirmStatusError", typeof(string));
			UnknownPriceFMTError = (string)configurationAppSettings.GetValue("FormalizeSettings.UnknownPriceFMTError", typeof(string));
			SheetsNotExistsError = (string)configurationAppSettings.GetValue("FormalizeSettings.SheetsNotExistsError", typeof(string));
			CostsNotExistsError = (string)configurationAppSettings.GetValue("FormalizeSettings.CostsNotExistsError", typeof(string));
			BaseCostNotExistsError = (string)configurationAppSettings.GetValue("FormalizeSettings.BaseCostNotExistsError", typeof(string));
			DoubleBaseCostsError = (string)configurationAppSettings.GetValue("FormalizeSettings.DoubleBaseCostsError", typeof(string));
			FieldNameBaseCostsError = (string)configurationAppSettings.GetValue("FormalizeSettings.FieldNameBaseCostsError", typeof(string));
			ZeroRollbackError = (string)configurationAppSettings.GetValue("FormalizeSettings.ZeroRollbackError", typeof(string));
			PrevFormRollbackError = (string)configurationAppSettings.GetValue("FormalizeSettings.PrevFormRollbackError", typeof(string));
			FieldsNotExistsError = (string)configurationAppSettings.GetValue("FormalizeSettings.FieldsNotExistsError", typeof(string));
			ParseMaskError = (string)configurationAppSettings.GetValue("FormalizeSettings.ParseMaskError", typeof(string));
			DoubleGroupMaskError = (string)configurationAppSettings.GetValue("FormalizeSettings.DoubleGroupMaskError", typeof(string));
			MinFieldCountError = (string)configurationAppSettings.GetValue("FormalizeSettings.MinFieldCountError", typeof(string));
			PeriodParseError = (string)configurationAppSettings.GetValue("FormalizeSettings.PeriodParseError", typeof(string));
			ThreadAbortError = (string)configurationAppSettings.GetValue("FormalizeSettings.ThreadAbortError", typeof(string));
			FileCopyError = (string)configurationAppSettings.GetValue("FormalizeSettings.FileCopyError", typeof(string));
			MaxErrorsError = (string)configurationAppSettings.GetValue("FormalizeSettings.MaxErrorsError", typeof(string));
			UnknowCostTypeError = (string)configurationAppSettings.GetValue("FormalizeSettings.UnknowCostTypeError", typeof(string));
			MulticolumnAsPriceError = (string)configurationAppSettings.GetValue("FormalizeSettings.MulticolumnAsPriceError", typeof(string));

		}

	}
}
