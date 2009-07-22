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
using RemotePricePricessor;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class RemotePricePricessorServiceFixture
	{
		private IRemotePriceProcessor priceProcessor;

		[SetUp]
		public void Setup()
		{
			ChannelServices.RegisterChannel(new HttpChannel(Settings.Default.RemotingPort), false);
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(RemotePricePricessorService),
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
			var history = priceProcessor.GetFileFormHistory(1);

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
	}
}

