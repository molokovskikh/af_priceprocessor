using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class Searcher
	{
		private readonly Hashtable _searchHash = new Hashtable();
		private readonly FieldInfo[] _indexFields;

		public Searcher(IEnumerable<ExistsCore> cores)
		{
			_indexFields = GetIndexFields();
			foreach (var core in cores) {
				var key = GetKey(core);
				if (_searchHash.ContainsKey(key))
					((List<Core>)_searchHash[key]).Add(core);
				else
					_searchHash.Add(key, new List<Core> { core });
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
			var type = typeof(Core);
			return indexFields.Select(f => type.GetField(f)).ToArray();
		}

		public ExistsCore Find(NewCore core)
		{
			var key = GetKey(core);
			if (!_searchHash.ContainsKey(key))
				return null;
			return ((List<Core>)_searchHash[key]).Cast<ExistsCore>().FirstOrDefault(c => c.NewCore == null);
		}

		private string GetKey(Core core)
		{
			var key = "";
			foreach (var field in _indexFields) {
				key += field.GetValue(core) + "-";
			}
			return key;
		}

		public DataRow[] Find(Core core)
		{
			var result = (List<DataRow>)_searchHash[GetKey(core)];
			if (result == null)
				return new DataRow[0];
			return result.ToArray();
		}
	}
}