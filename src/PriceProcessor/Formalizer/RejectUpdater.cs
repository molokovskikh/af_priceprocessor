using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using NHibernate;
using NHibernate.Linq;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class RejectUpdater
	{
		private IList<Reject> rejects = new List<Reject>();

		public void Process(FormalizationPosition position)
		{
			rejects.Add(new Reject(position));
		}

		public void Save(bool cancellations)
		{
			using (var scope = new TransactionScope(OnDispose.Rollback)) {
				var holder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = holder.CreateSession(typeof(ActiveRecordBase));
				try {
					var savedRejects = session.Query<Reject>().ToList();
					if (cancellations)
						Cancellations(session, savedRejects);
					else
						ProcessRejects(session, savedRejects);
				}
				finally {
					holder.ReleaseSession(session);
				}
				scope.VoteCommit();
			}
		}

		private void Cancellations(ISession session, List<Reject> savedRejects)
		{
			foreach (var savedReject in savedRejects) {
				if (rejects.Any(r => r.CheckCancellation(savedReject))) {
					session.Update(savedReject);
				}
			}
		}

		private void ProcessRejects(ISession session, List<Reject> savedRejects)
		{
			//порядок важен, тк не сохраненный запоминает что он совпал
			var forDelete = savedRejects.Where(r => !rejects.Any(n => n.Equivalent(r)));
			var forSave = rejects.Where(r => !savedRejects.Any(n => n.Equivalent(r)));

			foreach (var reject in forSave)
				session.Save(reject);

			foreach (var reject in forDelete)
				session.Delete(reject);
		}
	}
}