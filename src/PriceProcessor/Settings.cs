﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Configuration;

namespace Inforoom.PriceProcessor
{


	[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
	[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
	public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
	{

		private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

		public static Settings Default
		{
			get
			{
				return defaultInstance;
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("box.analit.net")]
		public string IMAPHost
		{
			get
			{
				return ((string)(this["IMAPHost"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("box.analit.net")]
		public string SMTPHost
		{
			get
			{
				return ((string)(this["SMTPHost"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("30")]
		public int HandlerRequestInterval
		{
			get
			{
				return ((int)(this["HandlerRequestInterval"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("30")]
		public int HandlerTimeout
		{
			get
			{
				return ((int)(this["HandlerTimeout"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Документы (накладные, отказы) не доставлены аптеке")]
		public string ResponseDocSubjectTemplateOnNothingAttachs
		{
			get
			{
				return ((string)(this["ResponseDocSubjectTemplateOnNothingAttachs"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Документы (накладные, отказы) невозможно доставить аптеке")]
		public string ResponseDocSubjectTemplateOnMultiDomen
		{
			get
			{
				return ((string)(this["ResponseDocSubjectTemplateOnMultiDomen"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Документы (накладные, отказы) невозможно доставить аптеке")]
		public string ResponseDocSubjectTemplateOnNonExistentClient
		{
			get
			{
				return ((string)(this["ResponseDocSubjectTemplateOnNonExistentClient"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("5")]
		public int FileDownloadInterval
		{
			get
			{
				return ((int)(this["FileDownloadInterval"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Документы (накладные, отказы) невозможно доставить аптеке")]
		public string ResponseDocSubjectTemplateOnBlockedProvider
		{
			get
			{
				return ((string)(this["ResponseDocSubjectTemplateOnBlockedProvider"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Документы (накладные, отказы) невозможно доставить аптеке")]
		public string ResponseDocSubjectTemplateOnUnknownProvider
		{
			get
			{
				return ((string)(this["ResponseDocSubjectTemplateOnUnknownProvider"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("60")]
		public int DepthOfStorageArchivePrices
		{
			get
			{
				return ((int)(this["DepthOfStorageArchivePrices"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("2")]
		public int ClearScanInterval
		{
			get
			{
				return ((int)(this["ClearScanInterval"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("1")]
		public int ASSORT_FLG
		{
			get
			{
				return ((int)(this["ASSORT_FLG"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Прайс не является ассортиментным, но не содержит ни одной цены.")]
		public string CostsNotExistsError
		{
			get
			{
				return ((string)(this["CostsNotExistsError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Не возможно сопоставить прайс файлу : ({0})")]
		public string UnknownPriceError
		{
			get
			{
				return ((string)(this["UnknownPriceError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Прайс отключен по причине : BillingStatus")]
		public string DisableByBillingStatusError
		{
			get
			{
				return ((string)(this["DisableByBillingStatusError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Прайс отключен по причине : FirmStatus")]
		public string DisableByFirmStatusError
		{
			get
			{
				return ((string)(this["DisableByFirmStatusError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Неизвестный формат прайса : ({0})")]
		public string UnknownPriceFMTError
		{
			get
			{
				return ((string)(this["UnknownPriceFMTError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Прайс не имеет базовой цены.")]
		public string BaseCostNotExistsError
		{
			get
			{
				return ((string)(this["BaseCostNotExistsError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Кол-во нулевых позиций выше допустимого.")]
		public string ZeroRollbackError
		{
			get
			{
				return ((string)(this["ZeroRollbackError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Кол-во формализованных позиций меньше кол-ва формализованных в прошлый раз.")]
		public string PrevFormRollbackError
		{
			get
			{
				return ((string)(this["PrevFormRollbackError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Не удалось разобрать маску имени {0}")]
		public string ParseMaskError
		{
			get
			{
				return ((string)(this["ParseMaskError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("В маски имени два раза встречается группа {0}.")]
		public string DoubleGroupMaskError
		{
			get
			{
				return ((string)(this["DoubleGroupMaskError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Кол-во полей, объявленных для распознания, меньше 1.")]
		public string MinFieldCountError
		{
			get
			{
				return ((string)(this["MinFieldCountError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Формализация прайса был остановленна.")]
		public string ThreadAbortError
		{
			get
			{
				return ((string)(this["ThreadAbortError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Не удалось скопировать файл {0} в каталог {1}.\\nПричина : {2}")]
		public string FileCopyError
		{
			get
			{
				return ((string)(this["FileCopyError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Файл {0} помещен в каталог {1} в результате {2} неудачных попыток формализации.")]
		public string MaxErrorsError
		{
			get
			{
				return ((string)(this["MaxErrorsError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Неизвестный тип ценовых колонок прайса : ({0})")]
		public string UnknowCostTypeError
		{
			get
			{
				return ((string)(this["UnknowCostTypeError"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("300")]
		public int MaxLiveTime
		{
			get
			{
				return ((int)(this["MaxLiveTime"]));
			}
		}

		/// <summary>
		/// По истечении этого времени, в лог заночится предупреждение о том, что прайс делается слишком долго
		/// </summary>
		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("20")]
		public int LongFormalizationWarningTimeout
		{
			get
			{
				return ((int)(this["LongFormalizationWarningTimeout"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("False")]
		public bool CheckZero
		{
			get
			{
				return ((bool)(this["CheckZero"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("2")]
		public int MaxErrorCount
		{
			get
			{
				return ((int)(this["MaxErrorCount"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("2")]
		public int MaxWorkThread
		{
			get
			{
				return ((int)(this["MaxWorkThread"]));
			}
			set { this["MaxWorkThread"] = value; }
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("100")]
		public int MaxRepeatTranCount
		{
			get
			{
				return ((int)(this["MaxRepeatTranCount"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("20")]
		public int MinRepeatTranCount
		{
			get
			{
				return ((int)(this["MinRepeatTranCount"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("250")]
		public int MaxPositionInsertToCore
		{
			get
			{
				return ((int)(this["MaxPositionInsertToCore"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("([^\\d]*(?<Day>\\d*?)[\\s\\.\\-\\/])?(?<Month>[^\\s\\.\\-\\/]*?)[\\s\\.\\-\\/](?<Year>\\d+)([^\\d" +
			"].*)?$")]
		public string DateMask
		{
			get
			{
				return ((string)(this["DateMask"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("farmsystem@analit.net")]
		public string FarmSystemEmail
		{
			get
			{
				return ((string)(this["FarmSystemEmail"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute(@"")]
		public string ResponseDocBodyTemplateOnNothingAttachs
		{
			get
			{
				return ((string)(this["ResponseDocBodyTemplateOnNothingAttachs"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute(@"Обслуживание Вашей организации не осуществляется. Просим Вас связаться с нами для выяснения причин.")]
		public string ResponseDocBodyTemplateOnBlockedProvider
		{
			get
			{
				return ((string)(this["ResponseDocBodyTemplateOnBlockedProvider"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute(@"")]
		public string ResponseDocBodyTemplateOnMultiDomen
		{
			get
			{
				return ((string)(this["ResponseDocBodyTemplateOnMultiDomen"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute(@"")]
		public string ResponseDocBodyTemplateOnNonExistentClient
		{
			get
			{
				return ((string)(this["ResponseDocBodyTemplateOnNonExistentClient"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute(@"")]
		public string ResponseDocBodyTemplateOnUnknownProvider
		{
			get
			{
				return ((string)(this["ResponseDocBodyTemplateOnUnknownProvider"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("3072")]
		public int MaxWaybillAttachmentSize
		{
			get
			{
				return ((int)(this["MaxWaybillAttachmentSize"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Размер документа (накладные, отказы) больше доспустимого значения")]
		public string ResponseDocSubjectTemplateOnMaxWaybillAttachment
		{
			get
			{
				return ((string)(this["ResponseDocSubjectTemplateOnMaxWaybillAttachment"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute(@"
")]
		public string ResponseDocBodyTemplateOnMaxWaybillAttachment
		{
			get
			{
				return ((string)(this["ResponseDocBodyTemplateOnMaxWaybillAttachment"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("888")]
		public int RemotingPort
		{
			get
			{
				return ((int)(this["RemotingPort"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("RemotePriceProcessor")]
		public string RemotingServiceName
		{
			get
			{
				return ((string)(this["RemotingServiceName"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("60")]
		public int AbortingThreadTimeout
		{
			get
			{
				return ((int)(this["AbortingThreadTimeout"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Inbound0")]
		public string InboundPath
		{
			get
			{
				return ((string)(this["InboundPath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("DownTemp")]
		public string TempPath
		{
			get
			{
				return ((string)(this["TempPath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("DownHistory")]
		public string HistoryPath
		{
			get
			{
				return ((string)(this["HistoryPath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Base")]
		public string BasePath
		{
			get
			{
				return ((string)(this["BasePath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("ErrorFiles")]
		public string ErrorFilesPath
		{
			get
			{
				return ((string)(this["ErrorFilesPath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("900")]
		public string WCFServicePort
		{
			get
			{
				return ((string)(this["WCFServicePort"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("RemotePriceProcessorService")]
		public string WCFServiceName
		{
			get
			{
				return ((string)(this["WCFServiceName"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("net.msmq://localhost/private/PriceProcessorWCFQueue")]
		public string WCFQueueName
		{
			get
			{
				return ((string)(this["WCFQueueName"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("143")]
		public string IMAPPort
		{
			get
			{
				return ((string)(this["IMAPPort"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("..\\..\\Data\\")]
		public string TestDataDirectory
		{
			get
			{
				return ((string)(this["TestDataDirectory"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string TestIMAPUser
		{
			get
			{
				return ((string)(this["TestIMAPUser"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("12345678")]
		public string TestIMAPPass
		{
			get
			{
				return ((string)(this["TestIMAPPass"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string SMTPUserFail
		{
			get
			{
				return ((string)(this["SMTPUserFail"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string SMTPErrorList
		{
			get
			{
				return ((string)(this["SMTPErrorList"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string DocumentFailMail
		{
			get
			{
				return ((string)(this["DocumentFailMail"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string UnrecLetterMail
		{
			get
			{
				return ((string)(this["UnrecLetterMail"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string SMTPWarningList
		{
			get
			{
				return ((string)(this["SMTPWarningList"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string ServiceMail
		{
			get
			{
				return ((string)(this["ServiceMail"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string WaybillIMAPUser
		{
			get
			{
				return ((string)(this["WaybillIMAPUser"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("12345678")]
		public string WaybillIMAPPass
		{
			get
			{
				return ((string)(this["WaybillIMAPPass"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string MailKitClientUser
		{
			get
			{
				return ((string)(this["MailKitClientUser"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("12345678")]
		public string MailKitClientPass
		{
			get
			{
				return ((string)(this["MailKitClientPass"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string IMAPUser
		{
			get
			{
				return ((string)(this["IMAPUser"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("12345678")]
		public string IMAPPass
		{
			get
			{
				return ((string)(this["IMAPPass"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
			"org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
			"tring>4596</string>\r\n  <string>2554</string>\r\n</ArrayOfString>")]
		public global::System.Collections.Specialized.StringCollection SyncPriceCodes
		{
			get
			{
				return ((global::System.Collections.Specialized.StringCollection)(this["SyncPriceCodes"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("FtpRoot")]
		public string FTPOptBoxPath
		{
			get
			{
				return ((string)(this["FTPOptBoxPath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("DocumentPath")]
		public string DocumentPath
		{
			get
			{
				return ((string)(this["DocumentPath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("IdxDir")]
		public string IdxDir
		{
			get
			{
				return ((string)(this["IdxDir"]));
			}
		}


		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("FtpRoot")]
		public string WaybillForParsePath
		{
			get
			{
				return ((string)(this["WaybillForParsePath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("DownWaybills")]
		public string DownWaybillsPath
		{
			get
			{
				return global::Common.Tools.FileHelper.MakeRooted((string)(this["DownWaybillsPath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("")]
		public string IMAPHandlerErrorMessageTo
		{
			get
			{
				return ((string)(this["IMAPHandlerErrorMessageTo"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("INBOX")]
		public string IMAPSourceFolder
		{
			get
			{
				return ((string)(this["IMAPSourceFolder"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Certificates")]
		public string CertificatePath
		{
			get
			{
				return ((string)(this["CertificatePath"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("KvasovTest@analit.net")]
		public string DocIMAPUser
		{
			get
			{
				return ((string)(this["DocIMAPUser"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("12345678")]
		public string DocIMAPPass
		{
			get
			{
				return ((string)(this["DocIMAPPass"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("Attachments")]
		public string AttachmentPath
		{
			get
			{
				return ((string)(this["AttachmentPath"]));
			}
        }

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public string RedmineAssignedTo
        {
            get
            {
                return ((string)(this["RedmineAssignedTo"]));
            }
        }
		[global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public string RedmineAssignedToWithPriority
		{
            get
            {
                return ((string)(this["RedmineAssignedToWithPriority"]));
            }
        }
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public string RedmineUrl
        {
            get
            {
                return ((string)(this["RedmineUrl"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public string RedmineProjectForWaybillIssue
        {
            get
            {
                return ((string)(this["RedmineProjectForWaybillIssue"]));
            }
        }
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public string RedmineProjectForWaybillIssueWithPriority
				{
            get
            {
                return ((string)(this["RedmineProjectForWaybillIssueWithPriority"]));
            }
        }
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public string RedmineKeyForWaybillIssue
        {
            get
            {
                return ((string)(this["RedmineKeyForWaybillIssue"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public string RedmineUrlFileUpload
        {
            get
            {
                return ((string)(this["RedmineUrlFileUpload"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("2")]
		public int MaxMiniMailSize
		{
			get
			{
				return ((int)(this["MaxMiniMailSize"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("2")]
		public int UIDProcessTimeout
		{
			get
			{
				return ((int)(this["UIDProcessTimeout"]));
			}
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		public int MySqlMaxPacketSize
		{
			get { return (int)this["MySqlMaxPacketSize"]; }
		}

		[ApplicationScopedSetting]
		public int MaxRetransThread
		{
			get { return (int)this["MaxRetransThread"]; }
			set { this["MaxRetransThread"] = value; }
		}

		[global::System.Configuration.ApplicationScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("")]
		public string IMAPUrl
		{
			get
			{
				return ((string)(this["IMAPUrl"]));
			}
		}
	}
}
