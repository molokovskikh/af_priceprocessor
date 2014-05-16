using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class ParserState
	{
		public string Tag;
		public ParserState Next;
		public ParserState Prev;
		public bool IsConsumable;
		public bool IsYield;

		public ParserState()
		{
		}

		public ParserState(string tag)
		{
			Tag = tag;
		}

		public ParserState(string tag, ParserState next)
		{
			Tag = tag;
			Next = next;
			next.Prev = this;
		}

		public virtual void Read(string tag, string value)
		{
		}

		public virtual void BeginConsume()
		{
		}

		public virtual object EndConsume()
		{
			return null;
		}
	}

	public class PriceItemState : ParserState
	{
		private FormalizationPosition _position;
		public List<Cost> Costs;

		public PriceItemState(string tag, ParserState next) : base(tag, next)
		{
			Costs = new List<Cost>();
			IsConsumable = true;
			IsYield = true;
		}

		public override void BeginConsume()
		{
			_position = new FormalizationPosition { Core = new NewCore() };
		}

		public override object EndConsume()
		{
			_position.Core.Costs = Costs.GroupBy(c => c.Description).Select(g => g.First()).ToArray();
			Costs.Clear();
			return _position;
		}

		public override void Read(string tag, string value)
		{
			var position = _position;
			var core = position.Core;
			if (tag.Match("Code")) {
				core.Code = value;
				return;
			}

			if (tag.Match("CodeCr")) {
				core.CodeCr = value;
				return;
			}

			if (tag.Match("Product")) {
				position.PositionName = value;
				return;
			}

			if (tag.Match("Producer")) {
				position.FirmCr = value;
				return;
			}

			if (tag.Match("Unit")) {
				core.Unit = value;
				return;
			}

			if (tag.Match("Volume")) {
				core.Volume = value;
				return;
			}

			if (tag.Match("Quantity")) {
				core.Quantity = value;
				return;
			}

			if (tag.Match("Note")) {
				core.Note = value;
				return;
			}

			if (tag.Match("Period")) {
				core.Period = value;
				return;
			}

			if (tag.Match("Doc")) {
				core.Doc = value;
				return;
			}

			if (tag.Match("Junk")) {
				if (value == "0")
					core.Junk = false;
				else if (value == "1")
					core.Junk = true;
				return;
			}

			if (tag.Match("Await")) {
				if (value == "0")
					core.Await = false;
				else if (value == "1")
					core.Await = true;
				return;
			}

			if (tag.Match("VitallyImportant")) {
				if (value == "0")
					core.VitallyImportant = false;
				else if (value == "1")
					core.VitallyImportant = true;
				return;
			}

			if (tag.Match("NDS")) {
				uint nds;
				if (uint.TryParse(value, out nds))
					core.Nds = nds;
				return;
			}

			if (tag.Match("MinBoundCost")) {
				decimal minBoindCost;
				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out minBoindCost))
					core.MinBoundCost = minBoindCost;
				return;
			}

			if (tag.Match("MaxBoundCost")) {
				decimal maxBoundCost;
				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out maxBoundCost))
					core.MaxBoundCost = maxBoundCost;
				return;
			}

			if (tag.Match("RegistryCost")) {
				decimal registryCost;
				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out registryCost))
					core.RegistryCost = registryCost;
				return;
			}

			if (tag.Match("ProducerCost")) {
				decimal producerCost;
				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out producerCost))
					core.ProducerCost = producerCost;
				return;
			}

			if (tag.Match("RequestRatio")) {
				uint requestRatio;
				if (uint.TryParse(value, out requestRatio))
					core.RequestRatio = requestRatio;
				return;
			}

			if (tag.Match("MinOrderSum")) {
				decimal minOrderSum;
				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out minOrderSum))
					core.OrderCost = minOrderSum;
				return;
			}

			if (tag.Match("MinOrderCount")) {
				uint minOrderCount;
				if (uint.TryParse(value, out minOrderCount))
					core.MinOrderCount = minOrderCount;
				return;
			}

			if (tag.Match("CodeOKP")) {
				core.CodeOKP = SafeConvert.ToUInt32(value);
				return;
			}

			if (tag.Match("EAN13")) {
				core.EAN13 = value;
				return;
			}

			if (tag.Match("Series")) {
				core.Series = value;
				return;
			}
		}
	}

	public class PriceCostState : ParserState
	{
		private List<CostDescription> _descriptions;
		private Cost _cost;
		private Dictionary<string, CostDescription> lookup;

		public PriceCostState(string tag, List<CostDescription> descriptions) : base(tag)
		{
			IsConsumable = true;
			_descriptions = descriptions;
			lookup = _descriptions.ToDictionary(h => h.Name, h => h);
		}

		public override void BeginConsume()
		{
			_cost = new Cost();
		}

		public override object EndConsume()
		{
			if (_cost.IsValid())
				((PriceItemState)Prev).Costs.Add(_cost);
			DescriptionOparation(_cost);
			return null;
		}

		private void DescriptionOparation(Cost cost)
		{
			var description = cost.Description;

			var value = cost.Value;

			if (value == 0)
				description.ZeroCostCount++;
			if (Cost.IsZeroOrLess(value)) {
				description.UndefinedCostCount++;
			}
		}

		public override void Read(string tag, string value)
		{
			if (tag.Match("Id")) {
				if (String.IsNullOrEmpty(value))
					return;

				if (!lookup.TryGetValue(value, out _cost.Description)) {
					var costDescription = new CostDescription { Name = value };
					_cost.Description = costDescription;
					_descriptions.Add(costDescription);
					lookup = _descriptions.ToDictionary(h => h.Name, h => h);
				}
				return;
			}

			if (tag.Match("Value")) {
				decimal cost;
				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out cost))
					_cost.Value = cost;
				return;
			}

			if (tag.Match("RequestRatio")) {
				uint requestRatio;
				if (uint.TryParse(value, out requestRatio))
					_cost.RequestRatio = requestRatio;
				return;
			}

			if (tag.Match("MinOrderSum")) {
				decimal minOrderSum;
				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out minOrderSum))
					_cost.MinOrderSum = minOrderSum;
				return;
			}

			if (tag.Match("MinOrderCount")) {
				uint minOrderCount;
				if (uint.TryParse(value, out minOrderCount))
					_cost.MinOrderCount = minOrderCount;
				return;
			}
		}
	}

	public class UniversalReader : IReader
	{
		private XmlReader _reader;
		private State _state = State.None;
		private object _item;

		public UniversalReader(Stream stream)
		{
			_reader = XmlReader.Create(stream);
			CostDescriptions = new List<CostDescription>();
		}

		public List<CostDescription> CostDescriptions { get; set; }

		public IEnumerable<FormalizationPosition> Read()
		{
			var state = new ParserState("", new ParserState("Price", new PriceItemState("Item", new PriceCostState("Cost", CostDescriptions))));
			var currentState = state;
			string valueTag = null;
			do {
				if (_reader.NodeType == XmlNodeType.Element) {
					if (currentState.Next != null && IsTag(currentState.Next.Tag)) {
						currentState = currentState.Next;
						if (currentState.IsConsumable) {
							currentState.BeginConsume();
						}
						continue;
					}

					if (currentState.IsConsumable) {
						valueTag = _reader.Name;
					}
				}
				else if (_reader.NodeType == XmlNodeType.Text) {
					if (currentState.IsConsumable)
						currentState.Read(valueTag, _reader.Value);
				}
				else if (_reader.NodeType == XmlNodeType.EndElement) {
					if (IsTag(currentState.Tag)) {
						object item = null;
						if (currentState.IsConsumable)
							item = currentState.EndConsume();
						if (currentState.IsYield && item != null)
							yield return (FormalizationPosition)item;

						if (currentState.Prev != null)
							currentState = currentState.Prev;
					}
				}
			} while (_reader.Read());
			yield break;
		}

		public enum State
		{
			None,
			Settings,
			Group,
			Address
		}

		public IEnumerable<Customer> Settings()
		{
			Customer customer = null;
			string valueTag = null;
			while (_reader.Read()) {
				if (_reader.NodeType == XmlNodeType.Element) {
					if (_state == State.None && IsTag("Price"))
						yield break;

					if (_state == State.None && IsTag("Settings")) {
						_state = State.Settings;
						continue;
					}

					if (_state == State.Settings && IsTag("Group")) {
						_state = State.Group;
						customer = new Customer();
						_item = customer;
						continue;
					}

					if (_state == State.Group) {
						if (IsTag("Address")) {
							_state = State.Address;
							var address = new AddressSettings();
							customer.Addresses.Add(address);
							_item = address;
							continue;
						}
						valueTag = _reader.Name;
					}

					if (_state == State.Address) {
						valueTag = _reader.Name;
					}
				}
				else if (_reader.NodeType == XmlNodeType.Text) {
					if (_item != null)
						ReadValue(_item, valueTag, _reader.Value);
				}
				else if (_reader.NodeType == XmlNodeType.EndElement) {
					if (_state == State.Group && IsTag("Group")) {
						_item = null;
						_state = State.Settings;
						if (customer != null)
							yield return customer;
						continue;
					}

					if (_state == State.Address && IsTag("Address")) {
						_item = null;
						_state = State.Group;
						continue;
					}

					if (_state == State.Settings && IsTag("Settings")) {
						_state = State.None;
						break;
					}
				}
			}
		}

		private void ReadValue(object item, string name, string value)
		{
			var customer = item as Customer;
			if (customer != null) {
				if (name.Match("ClientId")) {
					customer.SupplierClientId = value;
					return;
				}
				if (name.Match("PayerId")) {
					customer.SupplierPaymentId = value;
					return;
				}
				if (name.Match("CostId")) {
					customer.CostId = value;
					return;
				}
				if (name.Match("Markup")) {
					decimal markup;
					if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out markup))
						customer.PriceMarkup = markup;
					return;
				}
				if (name.Match("Available")) {
					if (value == "1")
						customer.Available = true;
					else if (value == "0")
						customer.Available = false;
					return;
				}
			}

			var address = item as AddressSettings;
			if (address != null) {
				if (name.Match("AddressId")) {
					address.SupplierAddressId = value;
					return;
				}
				if (name.Match("ControlMinReq")) {
					if (value == "1")
						address.ControlMinReq = true;
					else if (value == "0")
						address.ControlMinReq = false;
					return;
				}
				if (name.Match("MinReq")) {
					uint minReq;
					if (uint.TryParse(value, out minReq))
						address.MinReq = minReq;
					return;
				}
			}
		}

		private bool IsTag(string name)
		{
			return _reader.Name.Match(name);
		}

		public void SendWarning(FormLog stat)
		{
		}
	}
}