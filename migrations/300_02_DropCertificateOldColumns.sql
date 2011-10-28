alter table Documents.CertificateFiles
  drop column CertificateId,
  drop column SupplierId;

alter table Documents.CertificateTasks
  drop column SupplierId;
