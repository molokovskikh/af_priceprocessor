update
  documents.CertificateSources
set
  FtpFileDate = FtpFileDate - interval 1 day
where
  SourceClassName = 'RostaCertificateSource';