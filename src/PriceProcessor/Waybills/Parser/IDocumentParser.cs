namespace Inforoom.PriceProcessor.Waybills
{
	public interface IDocumentParser
	{
		Document Parse(string file, Document document);
	}
}