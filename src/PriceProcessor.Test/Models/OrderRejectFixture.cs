using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class RejectHeaderFixture
	{
		[Test]
		public void Parse()
		{
			var log = new DocumentReceiveLog(new Supplier(), new Address(new Client()));
			var reject = RejectHeader.ReadReject(log, @"..\..\data\rejects\35115498_Надежда-Фарм Орел_Фарма Орел(protocol).txt");
			Assert.AreEqual(1, reject.Lines.Count);
			var line = reject.Lines[0];
			Assert.AreEqual("Юниэнзим с МПС таб п/о N20", line.Product);
			Assert.AreEqual("Юникем Лабора", line.Producer);
			Assert.AreEqual(3, line.Rejected);
			Assert.AreEqual(0, line.Cost);
		}
	}
}