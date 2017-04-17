using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Inforoom.PriceProcessor.Formalizer.Core;

namespace Inforoom.PriceProcessor.Formalizer.Helpers
{
	public class Mapping
	{
		private static Lazy<Mapping[]> _offerMapping = new Lazy<Mapping[]>(GetOfferMapping, LazyThreadSafetyMode.PublicationOnly);
		private static Lazy<Mapping[]> _costMapping = new Lazy<Mapping[]>(GetCostMapping, LazyThreadSafetyMode.PublicationOnly);

		private FieldInfo field;

		public Mapping(FieldInfo info)
		{
			Name = info.Name;
			field = info;
		}

		public string Name { get; private set; }

		public bool Equal(object current, object old)
		{
			return Equals(field.GetValue(old), GetValue(current));
		}

		public object GetValue(object current)
		{
			var value = field.GetValue(current);
			if (value == null)
				return "";
			return value;
		}

		public void SetValue(object value, Core.Offer offer)
		{
			field.SetValue(offer, value);
		}

		public Type Type
		{
			get { return field.FieldType; }
		}

		public override string ToString()
		{
			return Name;
		}

		private static Mapping[] GetOfferMapping()
		{
			return typeof(Core.Offer).GetFields().Where(f => f.Name != "Costs").Select(f => new Mapping(f)).ToArray();
		}

		private static Mapping[] GetCostMapping()
		{
			return typeof(Cost).GetFields().Where(f => f.Name != "Description").Select(f => {
				var m = new Mapping(f);
				if (m.Name == "Value")
					m.Name = "Cost";
				return m;
			}).ToArray();
		}

		public static Mapping[] OfferMapping
		{
			get { return _offerMapping.Value; }
		}

		public static Mapping[] CostMapping
		{
			get { return _costMapping.Value; }
		}
	}
}