update
  documents.CertificateFiles,
  documents.CertificateSources
set
  CertificateFiles.ExternalFileId = CertificateFiles.OriginFileName
where
    CertificateFiles.CertificateSourceId = CertificateSources.Id
and CertificateFiles.ExternalFileId is null
and CertificateSources.SourceClassName = 'AptekaHoldingVoronezhCertificateSource';