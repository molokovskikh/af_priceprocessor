using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class FarmaimpeksOKPReader : IReader, IDisposable
	{
		private readonly string _filename;
		private readonly XmlReader _reader;
		private Stream _stream;

		public FarmaimpeksOKPReader(string filename)
		{
			_filename = filename;
			var settings = new XmlReaderSettings {
				IgnoreWhitespace = true
			};
			_stream = File.OpenRead(_filename);
			_reader = XmlReader.Create(_stream, settings);
		}

		public IEnumerable<FormalizationPosition> Read()
		{
			var read = false;
			while(_reader.Read()) {
				if(_reader.Name == "" && _reader.NodeType == XmlNodeType.Text && read) {
					read = false;
					var codeOkp = SafeConvert.ToUInt32(_reader.Value);
					if (codeOkp == 0)
						continue;
					yield return new FormalizationPosition {
						NotCreateUnrecExp = true,
						Status = UnrecExpStatus.NameForm,
						Core = new NewCore {
							CodeOKP = codeOkp
						}
					};
				}
				else if(_reader.Name == "CodeOKP") {
					read = true;
				}
			}
		}

		public List<CostDescription> CostDescriptions { get; set; }

		public IEnumerable<Customer> Settings()
		{
			return null;
		}

		public void SendWarning(PriceLoggingStat stat)
		{
		}

		public void Dispose()
		{
			_reader.Close();
			_stream.Close();
		}
	}
}
