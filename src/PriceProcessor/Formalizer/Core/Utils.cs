using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Common.Tools;
using log4net;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	//Все возможные поля прайса
	public enum PriceFields
	{
		[Description("Код")] Code,
		[Description("Код производителя")] CodeCr,
		[Description("Наименование 1")] Name1,
		[Description("Наименование 2")] Name2,
		[Description("Наименование 3")] Name3,
		[Description("Производитель")] FirmCr,
		[Description("Единица измерения")] Unit,
		[Description("Цеховая упаковка")] Volume,
		[Description("Количество")] Quantity,
		[Description("Примечание")] Note,
		[Description("Срок годности")] Period,
		[Description("Документ")] Doc,
		[Description("Цена производителя")] ProducerCost,
		[Description("Ставка НДС")] Nds,
		[Description("Цена минимальная")] MinBoundCost,
		[Description("Срок")] Junk,
		[Description("Ожидается")] Await,
		[Description("Оригинальное наименование")] OriginalName,
		[Description("Жизненно важный")] VitallyImportant,
		[Description("Кратность")] RequestRatio,
		[Description("Реестровая цена")] RegistryCost,
		[Description("Цена максимальная")] MaxBoundCost,
		[Description("Минимальная сумма")] OrderCost,
		[Description("Минимальное количество")] MinOrderCount,
		[Description("Код EAN-13 (штрих-код)")] EAN13,
		[Description("код ОКП")] CodeOKP,
		[Description("Серия")] Series
	}

	public enum CostTypes
	{
		MultiColumn = 0,
		MiltiFile = 1
	}

	//Статистические счетчики для формализации
	public class FormalizeStats
	{
		private ILog _logger = LogManager.GetLogger(typeof(FormalizeStats));

		//найдены по первичным полям
		public int FirstSearch;
		//найдены по остальным полям
		public int SecondSearch;
		//кол-во обновленных записей
		public int UpdateCount;
		//кол-во вставленных записей
		public int InsertCount;
		//кол-во удаленных записей
		public int DeleteCount;
		//кол-во обновленных цен
		public int UpdateCostCount;
		//кол-во добавленных цен
		public int InsertCostCount;
		//кол-во удаленных цен, не считаются цены, которые были удалены из удаления позиции из Core
		public int DeleteCostCount;
		//общее кол-во SQL-команд при обновлении прайс-листа
		public int CommandCount;
		//Среднее время поиска в миллисекундах записи в существующем прайсе
		public int AvgSearchTime;

		public int ProducerSynonymCreatedCount;

		public int ProducerSynonymUsedExistCount;

		private Stopwatch assortmentSearchWatch = new Stopwatch();
		private int assortmentSearchCount;
		private Stopwatch excludesSearchWatch = new Stopwatch();
		private int excludesSearchCount;


		public bool CanCreateProducerSynonyms()
		{
			return ProducerSynonymCreatedCount == 0 || ProducerSynonymCreatedCount < ProducerSynonymUsedExistCount || (ProducerSynonymUsedExistCount / (double)ProducerSynonymCreatedCount * 100 > 20);
		}

		//Сбросить счетчики, которые используются в статистике подготовки SQL-команд с update'ми
		public void ResetCountersForUpdate()
		{
			FirstSearch = 0;
			SecondSearch = 0;
			UpdateCount = 0;
			InsertCount = 0;
			DeleteCount = 0;
			UpdateCostCount = 0;
			InsertCostCount = 0;
			DeleteCostCount = 0;
			CommandCount = 0;
			AvgSearchTime = 0;
		}

		public string GetStatUpdateMessage()
		{
			var statCounterValues = new List<string>();
			foreach (var field in typeof(FormalizeStats).GetFields())
				statCounterValues.Add(String.Format("{0} = {1}", field.Name, field.GetValue(this)));
			return String.Format("Статистика обновления прайс-листа: {0}", String.Join("; ", statCounterValues.ToArray()));
		}

		public void PrintSearchStats()
		{
			_logger.DebugFormat(
				"Statistica search: assortment = {0} excludes = {1}  during assortment = {2} during excludes = {3}",
				(assortmentSearchCount > 0) ? assortmentSearchWatch.ElapsedMilliseconds / assortmentSearchCount : 0,
				(excludesSearchCount > 0) ? excludesSearchWatch.ElapsedMilliseconds / excludesSearchCount : 0,
				assortmentSearchWatch.ElapsedMilliseconds,
				excludesSearchWatch.ElapsedMilliseconds);
		}

		public IDisposable ExludeSearch()
		{
			excludesSearchCount++;
			excludesSearchWatch.Start();
			return new DisposibleAction(() => excludesSearchWatch.Stop());
		}

		public IDisposable AssortmentSearch()
		{
			assortmentSearchCount++;
			assortmentSearchWatch.Start();
			return new DisposibleAction(() => excludesSearchWatch.Stop());
		}
	}

	[Flags]
	public enum UnrecExpStatus : byte
	{
		NotForm = 0, // Неформализованный
		NameForm = 1, // Формализованный по названию
		FirmForm = 2, // Формализованный по производителю
		FullForm = NameForm | FirmForm, // Полностью формализован по наименованию, производителю и ассортименту
		MarkForb = 8, // Помеченый как запрещенное
		MarkExclude = 16, // Помеченый как исключение
	}

	//Класс содержит название полей из таблицы FormRules
	public sealed class FormRules
	{
		public static string colParserClassName = "ParserClassName";
		public static string colSelfPriceName = "SelfPriceName";
		public static string colFirmShortName = "FirmShortName";
		public static string colFirmCode = "FirmCode";
		public static string colFormByCode = "FormByCode";
		public static string colPriceCode = "PriceCode";
		public static string colPriceItemId = "PriceItemId";
		public static string colCostCode = "CostCode";
		public static string colParentSynonym = "ParentSynonym";
		public static string colNameMask = "NameMask";
		public static string colForbWords = "ForbWords";
		public static string colSelfAwaitPos = "SelfAwaitPos";
		public static string colSelfJunkPos = "SelfJunkPos";
		public static string colSelfVitallyImportantMask = "SelfVitallyImportantMask";
		public static string colPrevRowCount = "RowCount";
		public static string colPriceType = "PriceType";
		public static string colDelimiter = "Delimiter";
		public static string colBillingStatus = "BillingStatus";
		public static string colFirmStatus = "FirmStatus";
		public static string colCostType = "CostType";
		public static string colPriceFormatId = "PriceFormatId";
	}
}