using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class FarmaimpeksOKPReaderFixture
	{
		[Test]
		public void ReadPositionTest()
		{
			var reader = new FarmaimpeksOKPReader(@"..\..\Data\FarmimpeksOKP.xml");
			var positions = reader.Read().ToList();
			Assert.That(positions.Count, Is.EqualTo(4));
			Assert.That(positions[0].Core.CodeOKP, Is.EqualTo(931201));
		}

		[Test]
		public void NotSendNotImplementedException()
		{
			var reader = new FarmaimpeksOKPReader(@"..\..\Data\FarmimpeksOKP.xml");
			try {
				reader.SendWarning(null);
			}
			catch(NotImplementedException) {
				Assert.Fail("NotImplementedException выбрасывать не должны");
			}
		}
	}
}
