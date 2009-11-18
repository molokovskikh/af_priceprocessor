using System;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Text;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using RemotePriceProcessor;
using System.Runtime.Serialization.Formatters;
using System.Collections;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class RemotePricePricessorServiceFixture
	{
		private IRemotePriceProcessor priceProcessor;

		[TestFixtureSetUp]
		public void Setup()
		{
			//var http = new HttpChannel(Settings.Default.RemotingPort);
			var provider = new SoapServerFormatterSinkProvider();
			provider.TypeFilterLevel = TypeFilterLevel.Full;

			IDictionary props = new Hashtable();
			props["port"] = Settings.Default.RemotingPort;
			props["typeFilterLevel"] = TypeFilterLevel.Full;

			var channel = new HttpChannel(props, null, provider);
			ChannelServices.RegisterChannel(channel, false);
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(RemotePriceProcessorService),
				Settings.Default.RemotingServiceName,
				WellKnownObjectMode.Singleton);

			RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

			TestHelper.InitDirs(Settings.Default.HistoryPath, Settings.Default.BasePath);

			priceProcessor = (IRemotePriceProcessor) Activator.GetObject(typeof (IRemotePriceProcessor),
			                                                             "http://localhost:888/RemotePriceProcessor");
		}

		[Test]
		public void Get_file_form_history_should_return_file_content_and_name()
		{
			var file = Path.Combine(Settings.Default.HistoryPath, "1.txt");
			File.AppendAllText(file, "тест");
			var history = priceProcessor.GetFileFormHistory(new WcfCallParameter { Value = 1 });

			Assert.That(history.Filename, Is.EqualTo("1.txt"));
			Assert.That(new StreamReader(history.FileStream).ReadToEnd(), Is.EqualTo("тест"));
		}

		[Test]
		public void Get_file_from_base()
		{
			string filename = null;
			With.Connection(c =>
			                	{
			                		var command = new MySqlCommand(@"
select cast(concat(pim.Id, p.FileExtention) as CHAR)
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
limit 1", c);
			                		filename = command.ExecuteScalar().ToString();
			                	});

			filename = Path.Combine(Settings.Default.BasePath, filename);
			File.WriteAllText(filename, "тестовый прайс");

			var fileStream = priceProcessor.BaseFile(Convert.ToUInt32(Path.GetFileNameWithoutExtension(filename)));
			Assert.That(new StreamReader(fileStream).ReadToEnd(), Is.EqualTo("тестовый прайс"));
		}

		[Test]
		public void Put_file_to_base()
		{
			string filename = null;
			With.Connection(c =>
			{
				var command = new MySqlCommand(@"
select cast(concat(pim.Id, p.FileExtention) as CHAR)
from  usersettings.PriceItems pim
  join farm.formrules f on f.Id = pim.FormRuleId
  join farm.pricefmts p on p.ID = f.PriceFormatId
order by pim.Id desc
limit 1", c);
				filename = command.ExecuteScalar().ToString();
			});

			var priceItemId = Convert.ToUInt32(Path.GetFileNameWithoutExtension(filename));
			filename = Path.Combine(Settings.Default.BasePath, filename);

			if (File.Exists(filename))
				File.Delete(filename);

			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "тестовый прайс");

			using (var sendStream = File.OpenRead(tempFile))
			{
				sendStream.Position = sendStream.Length - 2;
                FilePriceInfo filePriceInfo = new FilePriceInfo();
                filePriceInfo.PriceItemId = priceItemId;
                filePriceInfo.Stream = sendStream;
				priceProcessor.PutFileToBase(filePriceInfo);
			}

			Assert.IsTrue(File.Exists(filename), "Файл не существует в Base");
			Assert.That(File.ReadAllText(filename), Is.EqualTo("тестовый прайс"), "Файл в Base отличается содержимым");			

			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}

	}
}

