DELETE
FROM
  documents.CERTIFICATEFILES;

DELETE
FROM
  documents.CERTIFICATES;

DELETE
FROM
  documents.CERTIFICATETASKS;

alter table Documents.CertificateFiles
  add column SupplierId int unsigned not null,
  add CONSTRAINT FK_CertificateFiles_SupplierId FOREIGN KEY (SupplierId) REFERENCES future.Suppliers (Id) ON DELETE cascade ON UPDATE cascade;

alter table Documents.CertificateTasks
  add column SupplierId int unsigned not null after Id;

alter table Documents.CertificateTasks
  add CONSTRAINT FK_CertificateTasks_SupplierId FOREIGN KEY (SupplierId) REFERENCES future.Suppliers (Id) ON DELETE cascade ON UPDATE cascade,
  add CONSTRAINT FK_CertificateTasks_CatalogId FOREIGN KEY (CatalogId) REFERENCES catalogs.catalog (Id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  add CONSTRAINT FK_CertificateTasks_DocumentBodyId FOREIGN KEY (DocumentBodyId) REFERENCES Documents.DocumentBodies (Id) ON DELETE RESTRICT ON UPDATE RESTRICT;

alter table Documents.CertificateTasks
  add UNIQUE KEY `IDX_CertificateTask` (SupplierId, CatalogId, SerialNumber);