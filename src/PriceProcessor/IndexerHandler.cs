﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using log4net;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using NHibernate.Linq;
using Directory = System.IO.Directory;
using Document = Lucene.Net.Documents.Document;

namespace Inforoom.PriceProcessor
{
	public struct SynonymInfo
	{
		public uint FirmCode;
		public string FirmName;
		public uint PriceCode;
		public uint ProductId;
		public bool Junk;
	}

	public class SynonymSummary
	{
		public SynonymSummary(string name)
		{
			_originalName = name;
			_summary = new List<SynonymInfo>();
		}

		public void AddInfo(uint firmcode, string firmname, uint pricecode, uint productId, bool junk)
		{
			if (_summary.Any(i => i.FirmCode == firmcode))
				return;
			var info = new SynonymInfo() { FirmCode = firmcode, FirmName = firmname, PriceCode = pricecode, ProductId = productId, Junk = junk };
			_summary.Add(info);
		}

		public IList<SynonymInfo> Summary()
		{
			return _summary;
		}

		public string OriginalName()
		{
			return _originalName;
		}

		private readonly string _originalName;
		private readonly IList<SynonymInfo> _summary;
	}

	public enum TaskState
	{
		None,
		Running,
		Success,
		Error,
		Canceled
	}

	public class SynonymTask
	{
		private readonly long id;
		private readonly uint priceCode; // код прайс-листа
		private readonly IList<string> names; // список позиций в прайс-листе
		private volatile bool stopped;


		private readonly Dictionary<string, SynonymSummary> matches;
		private IndexerHandler handler;

		private readonly Thread thread;

		//время прерывания рабочей нитки
		private DateTime? _abortingTime;

		private readonly ILog _logger = LogManager.GetLogger(typeof(SynonymTask));

		public long Id
		{
			get { return id; }
		}

		public Dictionary<string, SynonymSummary> Matches
		{
			get { return matches; }
		}

		public TaskState State { get; private set; }

		public DateTime StartDate { get; private set; }

		public DateTime? StopDate { get; private set; }

		public string Error { get; private set; }

		public uint Rate { get; private set; } // процент выполнения задачи

		public SynonymTask(IList<string> names, uint pricecode, IndexerHandler owner)
		{
			State = TaskState.None;
			priceCode = pricecode;
			this.names = names;
			handler = owner;
			id = DateTime.Now.Ticks;
			matches = new Dictionary<string, SynonymSummary>();
			thread = new Thread(ThreadWork);
			StartDate = DateTime.UtcNow;
			Rate = 0;
			thread.Start();
			State = TaskState.Running;
		}

		public ThreadState ThreadState
		{
			get { return thread.ThreadState; }
		}

		public bool ThreadIsAlive
		{
			get { return thread.IsAlive; }
		}

		public void InterruptThread()
		{
			thread.Interrupt();
		}

		public bool IsAbortingLong
		{
			get
			{
				return (
					(((thread.ThreadState & ThreadState.AbortRequested) > 0) || ((thread.ThreadState & ThreadState.Aborted) > 0))
						&& _abortingTime.HasValue
						&& (DateTime.UtcNow.Subtract(_abortingTime.Value).TotalSeconds > Settings.Default.AbortingThreadTimeout));
			}
		}

		public void Stop()
		{
			stopped = true;
		}

		/// <summary>
		/// останавливаем рабочую нитку и выставляем время останова, чтобы обрубить по таймауту
		/// </summary>
		public void AbortThread()
		{
			if (!_abortingTime.HasValue) {
				thread.Abort();
				_abortingTime = DateTime.UtcNow;
			}
		}

		public void ThreadWork()
		{
			try {
				DoMatching();
			}
			catch (Exception ex) {
				Error = ex.ToString();
				State = TaskState.Error;
				_logger.Error("Ошибка при сопоставлении синонимов. {0}", ex);
			}
		}

		private void DoMatching()
		{
			_logger.InfoFormat("Старт сопоставления для {0} позиций", names.Count());
			var idxDirectory = FSDirectory.Open(new DirectoryInfo(handler.IdxDir));
			var reader = IndexReader.Open(idxDirectory, true);
			var searcher = new IndexSearcher(reader);
			var analyzer = new KeywordAnalyzer();
			var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "Synonym", analyzer);
			uint counter = 0;
			try {
				foreach (var position in names) {
					if (stopped) {
						_logger.Info("Сопоставление отменено");
						State = TaskState.Canceled;
						return;
					}
					string name = position.Trim().ToUpper().Replace("\"", "_QUOTE_").Replace("\\", "_LSLASH_"); // почуму-то KeywordAnalyzer не находит фразы, если в них есть кавычки

					var query = parser.Parse(String.Format("Synonym:\"{0}\"", name));
					name = name.Replace("_QUOTE_", "\"").Replace("_LSLASH_", "\\");
					if (matches.ContainsKey(name))
						continue;
					var collector = TopScoreDocCollector.create(10000, true);
					searcher.Search(query, collector);
					var hits = collector.TopDocs().scoreDocs;
					foreach (var scoreDoc in hits) {
						var document = searcher.Doc(scoreDoc.doc);
						var pcode = Convert.ToUInt32(document.Get("PriceCode"));
						// если уже существует синоним с таким PriceCode - не добавляем в результирующий набор
						if (priceCode == pcode) {
							if (matches.ContainsKey(name))
								matches.Remove(name);
							break;
						}
						if (!matches.ContainsKey(name))
							matches[name] = new SynonymSummary(position);
						matches[name].AddInfo(Convert.ToUInt32(document.Get("FirmCode")),
							document.Get("FirmName"),
							Convert.ToUInt32(document.Get("PriceCode")),
							Convert.ToUInt32(document.Get("ProductId")),
							Convert.ToBoolean(document.Get("Junk")));
					}
					counter++;
					Rate = (uint)(counter * 100 / names.Count());
				}
			}
			finally {
				reader.Close();
				searcher.Close();
				analyzer.Close();
				idxDirectory.Close();
				StopDate = DateTime.UtcNow;
			}
			State = TaskState.Success;
			_logger.Info("Сопоставление завершено");
		}
	}

	public class IndexerHandler : AbstractHandler
	{
		protected TimeSpan workTime;
		protected TimeSpan now;
		public string IdxDir { get; protected set; }

		protected IList<SynonymTask> taskList;

		public long AddTask(IList<string> names, uint pricecode)
		{
			var task = new SynonymTask(names, pricecode, this);
			lock (taskList) {
				taskList.Add(task);
			}
			return task.Id;
		}

		public SynonymTask GetTask(long taskId)
		{
			SynonymTask res = null;
			lock (taskList) {
				foreach (var task in taskList) {
					if (task.Id == taskId) {
						res = task;
						break;
					}
				}
			}
			return res;
		}

		private void ProcessTaskList()
		{
			lock (taskList) {
				for (int i = taskList.Count - 1; i >= 0; i--) {
					var task = taskList[i];
					if (task.StopDate != null) {
						if (DateTime.UtcNow.Subtract(task.StopDate.Value).TotalMinutes < 5)
							continue;
					}
					if (task.StopDate != null || !task.ThreadIsAlive || ((task.ThreadState & ThreadState.Stopped) > 0))
						taskList.RemoveAt(i);
					else if ((DateTime.UtcNow.Subtract(task.StartDate).TotalMinutes > 90) &&
						((task.ThreadState & ThreadState.AbortRequested) == 0)) {
						task.AbortThread();
					}
					else if (task.IsAbortingLong) {
						taskList.RemoveAt(i);
					}
					else if (((task.ThreadState & ThreadState.AbortRequested) > 0) &&
						((task.ThreadState & ThreadState.WaitSleepJoin) > 0)) {
						task.InterruptThread();
					}
				}
			}
		}

		public IndexerHandler()
		{
			SleepTime = 60;
			workTime = TimeSpan.Parse("02:00:00");
			taskList = new List<SynonymTask>();
			IdxDir = global::Common.Tools.FileHelper.MakeRooted(Settings.Default.IdxDir);
		}

		//Остановка обработчика
		public override void HardStop()
		{
			base.HardStop();

			if (!tWork.Join(JoinTimeout))
				_logger.ErrorFormat("Рабочая нитка не остановилась за {0} миллисекунд.", JoinTimeout);

			//Пытаемся корректно остановить нитки
			for (int i = taskList.Count - 1; i >= 0; i--)
				taskList[i].Stop();

			Thread.Sleep(1000);

			//Сначала для всех ниток вызваем Abort,
			for (int i = taskList.Count - 1; i >= 0; i--)
				//Если нитка работает, то останавливаем ее
				if (taskList[i].ThreadIsAlive) {
					taskList[i].AbortThread();
					_logger.InfoFormat("Вызвали Abort() для нитки {0}", taskList[i].Id);
				}

			//а потом ждем их завершения
			for (int i = taskList.Count - 1; i >= 0; i--) {
				if (!taskList[i].ThreadIsAlive)
					continue;

				//Если нитка работает, то ожидаем ее останов
				_logger.InfoFormat("Ожидаем останов нитки {0}", taskList[i].Id);
				taskList[i].AbortThread();
				var currentWaitTime = 0;
				while ((currentWaitTime < JoinTimeout) && ((taskList[i].ThreadState & ThreadState.Stopped) == 0)) {
					if ((taskList[i].ThreadState & ThreadState.WaitSleepJoin) > 0)
						taskList[i].InterruptThread();
					Thread.Sleep(1000);
					currentWaitTime += 1000;
				}
				if ((taskList[i].ThreadState & ThreadState.Stopped) > 0)
					_logger.InfoFormat("Останов нитки выполнен {0}", taskList[i].Id);
				else
					_logger.InfoFormat("Нитка сопоставления {0} не остановилась за {1} миллисекунд.", taskList[i].Id, JoinTimeout);
			}
		}

		protected bool CanDoIndex()
		{
			var diff = now.TotalSeconds - workTime.TotalSeconds;
			if (diff > 0 && diff <= SleepTime)
				return true;
			return false;
		}

		public override void ProcessData()
		{
			if (!Directory.Exists(IdxDir)) // если при запуске индекса нет - делаем его
				DoIndex();
			now = DateTime.Now.TimeOfDay;
			ProcessTaskList();
			if (!CanDoIndex())
				return;
			// производим индексацию данных
			DoIndex();
		}

		protected bool DoIndex(IList<ProductSynonym> synonyms, bool append, bool optimize)
		{
			if (append && !Directory.Exists(IdxDir))
				return false;
			var idxDirectory = FSDirectory.Open(new DirectoryInfo(IdxDir));
			var analyzer = new KeywordAnalyzer();
			var writer = new IndexWriter(idxDirectory, analyzer, !append, IndexWriter.MaxFieldLength.UNLIMITED);
			try {
				_logger.Info("Старт индексации синонимов...");
				foreach (var synonym in synonyms) {
					var synstr = synonym.Synonym.Replace("\"", "_QUOTE_").Replace("\\", "_LSLASH_");
					var doc = new Document();
					doc.Add(
						new Field(
							"FirmCode",
							synonym.Price.Supplier.Id.ToString(),
							Field.Store.YES,
							Field.Index.NO));
					doc.Add(
						new Field(
							"FirmName",
							synonym.Price.Supplier.Name + " (" + synonym.Price.Supplier.FullName + ")",
							Field.Store.YES,
							Field.Index.NO));
					doc.Add(
						new Field(
							"PriceCode",
							synonym.Price.Id.ToString(),
							Field.Store.YES,
							Field.Index.NO));
					doc.Add(
						new Field(
							"ProductId",
							synonym.Product.Id.ToString(),
							Field.Store.YES,
							Field.Index.NO));
					doc.Add(
						new Field(
							"Junk",
							synonym.Junk.ToString(),
							Field.Store.YES,
							Field.Index.NO));
					doc.Add(
						new Field(
							"Synonym",
							synstr.Trim().ToUpper(),
							Field.Store.YES,
							Field.Index.ANALYZED));
					writer.AddDocument(doc);
				}
				if (optimize) {
					_logger.Info("Оптимизация индекса...");
					writer.Optimize();
				}
			}
			finally {
				writer.Close();
				analyzer.Close();
				idxDirectory.Close();
			}
			_logger.Info("Индексация завершена");
			return true;
		}

		protected void DoIndex()
		{
			if (Directory.Exists(IdxDir))
				Directory.Delete(IdxDir, true);
			_logger.Info("Загрузка синонимов из БД...");
			IList<ProductSynonym> synonyms;
			using(var session = SessionHelper.GetSessionFactory().OpenSession()) {
				synonyms = session.Query<ProductSynonym>().Where(s => s.Synonym != null).ToList();
			}
			_logger.InfoFormat("Загрузили {0} синонимов", synonyms.Count());
			DoIndex(synonyms, false, true);
		}

		public void AppendToIndex(IList<int> ids)
		{
			if (ids.Count == 0)
				return;
			_logger.Info("Добавление синонимов к индексу...");
			_logger.Info("Загрузка синонимов из БД...");
			IList<ProductSynonym> synonyms;
			using(var session = SessionHelper.GetSessionFactory().OpenSession()) {
				synonyms = session.Query<ProductSynonym>().Where(s => ids.Contains(s.SynonymCode) && s.Product != null).ToList();
			}
			_logger.InfoFormat("Загрузили {0} синонимов", synonyms.Count());
			var res = DoIndex(synonyms, true, false);
			if (res)
				_logger.Info("Добавление завершено...");
			else
				_logger.Info("Ошибка при добавлении");
		}

		public static string[] TransformToStringArray(Dictionary<string, SynonymSummary> matches)
		{
			var result = new string[matches.Count + 1];
			result[0] = "Success";
			var i = 1;
			foreach (var key in matches) {
				var res = String.Empty;
				var summary = key.Value.Summary();
				res += summary.Count.ToString();
				res += ";";
				foreach (var synonymInfo in summary) {
					res += synonymInfo.FirmCode.ToString();
					res += ";";
					res += synonymInfo.FirmName;
					res += ";";
					res += synonymInfo.ProductId.ToString();
					res += ";";
					res += synonymInfo.Junk.ToString();
					res += ";";
				}
				res += key.Value.OriginalName();
				result[i] = res;
				i++;
			}
			return result;
		}
	}
}