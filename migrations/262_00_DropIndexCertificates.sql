alter table Documents.CertificateTasks
  drop FOREIGN KEY FK_CertificateTasks_CatalogId,
  drop FOREIGN KEY FK_CertificateTasks_DocumentBodyId;

alter table Documents.CertificateTasks
  drop index IDX_CertificateTask;

alter table Documents.CertificateTasks
  drop index FK_CertificateTasks_DocumentBodyId;
