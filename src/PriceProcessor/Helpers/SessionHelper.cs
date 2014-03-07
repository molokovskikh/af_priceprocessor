using System;
using System.Collections;
using System.Collections.Generic;
using Castle.ActiveRecord;
using NHibernate;

namespace Inforoom.PriceProcessor.Helpers
{
	public class SessionHelper
	{
		public static void Evict(ISession session, IEnumerable items)
		{
			foreach (var item in items)
				session.Evict(item);
		}

		public static void WithSession(Action<ISession> sessionDelegate)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try {
				sessionDelegate(session);
			}
			finally {
				sessionHolder.ReleaseSession(session);
			}
		}

		public static IList<T> WithSession<T>(Func<ISession, IList<T>> sessionDelegate)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try {
				var result = sessionDelegate(session);
				Evict(session, result);
				return result;
			}
			finally {
				sessionHolder.ReleaseSession(session);
			}
		}

		public static T WithSession<T>(Func<ISession, T> sessionDelegate)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try {
				T result = sessionDelegate(session);
				return result;
			}
			finally {
				sessionHolder.ReleaseSession(session);
			}
		}

		public static void StartSession(Action<ISession> action)
		{
			using(var scope = new TransactionScope(OnDispose.Rollback)) {
				WithSession(action);
				scope.VoteCommit();
			}
		}

		public static ISessionFactory GetSessionFactory()
		{
			return ActiveRecordMediator.GetSessionFactoryHolder()
				.GetSessionFactory(typeof(ActiveRecordBase));
		}
	}
}