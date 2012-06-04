using System;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class RejectFixture
	{
		[Test]
		public void Mark_if_canceled_only_for_date_after_reject()
		{
			var reject = new Reject {
				ProductId = 1,
				Series = "112012",
				LetterDate = new DateTime(2012, 4, 1)
			};
			var cancelation = new Reject {
				ProductId = 1,
				Series = "112012",
				LetterDate = new DateTime(2012, 3, 1)
			};
			Assert.That(cancelation.CheckCancellation(reject), Is.False);
			Assert.That(reject.CancelDate, Is.Null);
			cancelation.LetterDate = new DateTime(2012, 5, 1);
			Assert.That(cancelation.CheckCancellation(reject), Is.True);
			Assert.That(reject.CancelDate, Is.EqualTo(new DateTime(2012, 5, 1)));
		}
	}
}