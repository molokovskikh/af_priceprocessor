using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class FarmaimpeksReader : IReader, IDisposable
	{
		private readonly string _filename;
		private readonly XmlReader _reader;
		private bool _inPrice;
		private bool _readed;
		private Stream _stream;

		public FarmaimpeksReader(string filename)
		{
			_filename = filename;
			var settings = new XmlReaderSettings {
				IgnoreWhitespace = true
			};
			_stream = File.OpenRead(_filename);
			_reader = XmlReader.Create(_stream, settings);
		}

		public void Dispose()
		{
			_reader.Close();
			_stream.Close();
		}

		public IEnumerable<FarmaimpeksPrice> Prices()
		{
			while (ReadFromReader())
			{
				while (_reader.Name == "�����" && _reader.NodeType == XmlNodeType.Element && _readed)
				{
					_inPrice = true;
					_readed = false;
					yield return new FarmaimpeksPrice {
						Id = _reader.GetAttribute("ID"),
						Name = _reader.GetAttribute("������������")
					};
				}
			}
		}

		public IEnumerable<Customer> Settings()
		{
			do
			{
				if (_reader.Name == "����������" && _reader.NodeType == XmlNodeType.Element)
				{
					yield return new Customer {
						SupplierClientId = _reader.GetAttribute("����������ID"),
						PriceMarkup = Convert.ToDecimal(_reader.GetAttribute("�������"))
					};
				}
				else if (_reader.Name != "����������" && _reader.NodeType == XmlNodeType.Element)
				{
					yield break;
				}
			} while (ReadFromReader());
		}

		public IEnumerable<FormalizationPosition> Read()
		{
			var cost = CostDescriptions.First();
			while (ReadFromReader())
			{
				if (_inPrice && _reader.Name == "�������" && _reader.NodeType == XmlNodeType.Element)
				{
					yield return new FormalizationPosition {
						PositionName = _reader.GetAttribute("������������"),
						FirmCr = _reader.GetAttribute("�������������"),
						Core = new NewCore {
							Code = _reader.GetAttribute("���������"),
							CodeCr = _reader.GetAttribute("����������������"),
							MaxBoundCost = GetNullable<decimal>("��������"),
							Quantity = _reader.GetAttribute("����������"),
							Volume = _reader.GetAttribute("��������"),
							Period = _reader.GetAttribute("������������"),
							RegistryCost = GetNullable<decimal>("������"),
							RequestRatio = GetNullable<uint>("���������"),
							OrderCost = GetNullable<decimal>("��������"),
							MinOrderCount = GetNullable<uint>("�������������"),
							Await = _reader.GetAttribute("�������") == "��",
							Junk = _reader.GetAttribute("��������") == "��",
							VitallyImportant = _reader.GetAttribute("����") == "��",
							Costs = new[] {new Cost(cost, GetNullable<decimal>("����")),},
						}
					};
				}
				else if (_reader.Name != "�������" && _reader.NodeType == XmlNodeType.Element)
				{
					_inPrice = false;
					yield break;
				}
			}
		}

		private bool ReadFromReader()
		{
			_readed = true;
			return _reader.Read();
		}

		private T GetNullable<T>(string name)
		{
			var value = _reader.GetAttribute(name);
			if (String.IsNullOrWhiteSpace(value))
				return default(T);
			return (T)Convert.ChangeType(value, typeof (T), CultureInfo.InvariantCulture);
		}

		public List<CostDescription> CostDescriptions { get; set; }

		public void SendWarning(PriceLoggingStat stat)
		{}
	}
}