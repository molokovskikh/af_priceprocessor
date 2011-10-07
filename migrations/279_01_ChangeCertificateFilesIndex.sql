alter table Documents.CertificateFiles
  drop FOREIGN KEY FK_CertificateFiles_CertificateId,
  drop FOREIGN KEY FK_CertificateFiles_SupplierId;


alter table Documents.CertificateFiles
  drop index FK_CertificateFiles_CertificateId;

alter table Documents.CertificateFiles
  drop index FK_CertificateFiles_SupplierId;


alter table Documents.CertificateFiles
  add column ExternalFileId varchar(255) default null,
  add column CertificateSourceId int unsigned default null,
  modify column CertificateId int unsigned default null,
  modify column SupplierId int unsigned default null,
  add CONSTRAINT FK_CertificateFiles_CertificateSourceId FOREIGN KEY (CertificateSourceId) REFERENCES Documents.CertificateSources (Id) ON DELETE cascade ON UPDATE cascade,
  add index `IDX_CertificateFiles_ExternalFileId` (ExternalFileId);

update Documents.CertificateFiles
set
  ExternalFileId = OriginFileName,
  CertificateSourceId = 1
where
  SupplierId = 39;