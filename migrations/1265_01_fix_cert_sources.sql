update Documents.CertificateSources
set DecodeTableUrl = 'ftp://FTP_ANALIT:36AzQA63@orel.katren.ru:99/serts/table.dbf'
where SourceClassName = 'KatrenSource';

update Documents.CertificateSources
set DecodeTableUrl = 'ftp://ftpanalit:imalit76@ftp.apteka-raduga.ru:21/LIST/SERT_LIST.DBF'
where SourceClassName = 'RostaCertificateSource';
