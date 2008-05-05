using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.Formalizer;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceFieldsTest
	{
		public static string GetDescription(PriceFields value)
		{
			object[] descriptions = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
			return ((System.ComponentModel.DescriptionAttribute)descriptions[0]).Description;
		}

		[Test]
		public void GetEnumDescriptionTest()
		{
			Console.WriteLine("Description {0} : {1}", PriceFields.MaxBoundCost, GetDescription(PriceFields.MaxBoundCost));
			//PriceFields
		}
	}
}
