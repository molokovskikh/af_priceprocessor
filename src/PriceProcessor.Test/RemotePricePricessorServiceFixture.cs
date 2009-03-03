using System;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Text;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using RemotePricePricessor;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class RemotePricePricessorServiceFixture
	{
		[SetUp]
		public void Setup()
		{
			ChannelServices.RegisterChannel(new HttpChannel(Settings.Default.RemotingPort), false);
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(RemotePricePricessorService),
				Settings.Default.RemotingServiceName,
				WellKnownObjectMode.Singleton);
			RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

			TestHelper.InitDirs(Settings.Default.HistoryPath);
		}

		[Test]
		public void Get_file_form_history_should_return_file_content_and_name()
		{
			File.AppendAllText(Path.Combine(Settings.Default.HistoryPath, "1.txt"), "тест", Encoding.GetEncoding(1251));
			var priceProcessor = (IRemotePriceProcessor) Activator.GetObject(typeof (IRemotePriceProcessor),
			                                                                 "http://localhost:888/RemotePriceProcessor");
			var history = priceProcessor.GetFileFormHistory(1);

			Assert.That(history.Filename, Is.EqualTo("1.txt"));
			Assert.That(Encoding.GetEncoding(1251).GetString(history.Bytes), Is.EqualTo("тест"));
		}
	}
}

