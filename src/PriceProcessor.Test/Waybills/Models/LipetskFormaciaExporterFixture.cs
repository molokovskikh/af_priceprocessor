using System;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Models
{
	[TestFixture]
	public class LipetskFormaciaExporterFixture : IntegrationFixture
	{
		[Test]
		public void LipetskFormaciaExporter()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\446406_0.dbf");
			ExcelExporter.SaveLipetskFarmacia(document, "./waybill.xls");
		}
	}
}