alter table Documents.CertificateTasks
  add CONSTRAINT FK_CertificateTasks_CertificateSourceId FOREIGN KEY (CertificateSourceId) REFERENCES Documents.CertificateSources (Id) ON DELETE cascade ON UPDATE cascade,
  add CONSTRAINT FK_CertificateTasks_CatalogId FOREIGN KEY (CatalogId) REFERENCES catalogs.catalog (Id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  add CONSTRAINT FK_CertificateTasks_DocumentBodyId FOREIGN KEY (DocumentBodyId) REFERENCES Documents.DocumentBodies (Id) ON DELETE RESTRICT ON UPDATE RESTRICT;


alter table Documents.CertificateTasks
  add UNIQUE KEY `IDX_CertificateTask` (CertificateSourceId, CatalogId, SerialNumber);