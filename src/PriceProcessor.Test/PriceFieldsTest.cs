using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
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
			Assert.AreEqual("Цена максимальная", GetDescription(PriceFields.MaxBoundCost), "Полученное значение Description некорректно.");
		}
	}
}