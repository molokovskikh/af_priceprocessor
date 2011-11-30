namespace Inforoom.PriceProcessor.Downloader
{
	public class RostaCertificateCatalogHandler : AbstractHandler
	{
		public RostaCertificateCatalogHandler()
		{
			SleepTime = 5;
		}
		
		protected override void ProcessData()
		{
			//using (new SessionScope()) {
			//    var tasks = CertificateTask.FindAll();

			//    if (tasks != null && tasks.Length > 0)
			//        foreach (var certificateTask in tasks) {
			//            try {
			//                ProcessTask(certificateTask);
			//            }
			//            catch (Exception exception) {
			//                _logger.WarnFormat("Ошибка при отбработки задачи для сертификата {0} : {1}", certificateTask, exception);
			//            }
					
			//        }
			//}
		}
	}
}