alter table Documents.CertificateSources
change column FtpFileDate  LastDecodeTableDownload datetime COMMENT 'дата изменения файла, скаченного по ftp';


update Documents.CertificateSources
set LookupUrl = 'ftp://FTP_ANALIT:36AzQA63@orel.katren.ru:99/serts/'
where SourceClassName = 'KatrenSource';

update Documents.CertificateSources
set LookupUrl = 'ftp://ftpanalit:imalit76@ftp.apteka-raduga.ru:21/LIST/'
where SourceClassName = 'RostaCertificateSource';
