using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class Searcher
	{
		private readonly Hashtable _searchHash = new Hashtable();
		private readonly FieldInfo[] _indexFields;

		public Searcher(IEnumerable<ExistsOffer> cores)
		{
			_indexFields = GetIndexFields();
			foreach (var core in cores) {
				var key = GetKey(core);
				if (_searchHash.ContainsKey(key))
					((List<Offer>)_searchHash[key]).Add(core);
				else
					_searchHash.Add(key, new List<Offer> { core });
			}
		}

		public Searcher(IEnumerable<ExistsOffer> cores, FieldInfo[] fields)
		{
			_indexFields = fields;
			foreach (var core in cores) {
				var key = GetKey(core);
				if (_searchHash.ContainsKey(key))
					((List<Offer>)_searchHash[key]).Add(core);
				else
					_searchHash.Add(key, new List<Offer> { core });
			}
		}

		private FieldInfo[] GetIndexFields()
		{
			var indexFields = new[] {
				"ProductId",
				"CodeFirmCr",
				"SynonymCode",
				"SynonymFirmCrCode",
				"Code",
				"Junk",
				"Period",
				"RequestRatio",
				"OrderCost",
				"MinOrderCount"
			};
			var type = typeof(Offer);
			return indexFields.Select(f => type.GetField(f)).ToArray();
		}

		public ExistsOffer Find(NewOffer offer)
		{
			var key = GetKey(offer);
			if (!_searchHash.ContainsKey(key))
				return null;
			return ((List<Offer>)_searchHash[key]).Cast<ExistsOffer>().FirstOrDefault(c => c.NewOffer == null);
		}

		private string GetKey(Offer offer)
		{
			var key = "";
			foreach (var field in _indexFields) {
				key += field.GetValue(offer) + "-";
			}
			return key;
		}

		public DataRow[] Find(Offer offer)
		{
			var result = (List<DataRow>)_searchHash[GetKey(offer)];
			if (result == null)
				return new DataRow[0];
			return result.ToArray();
		}
	}
}