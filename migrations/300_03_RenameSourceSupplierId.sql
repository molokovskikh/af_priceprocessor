alter table Documents.CertificateSources
  drop foreign key FK_CertificateSources_SourceSupplierId;

alter table Documents.CertificateSources
  drop index FK_CertificateSources_SourceSupplierId;

alter table Documents.CertificateSources
  change SourceSupplierId FtpSupplierId int unsigned default null;

alter table Documents.CertificateSources
  add CONSTRAINT FK_CertificateSources_FtpSupplierId FOREIGN KEY (FtpSupplierId) REFERENCES future.Suppliers (Id) ON DELETE RESTRICT ON UPDATE RESTRICT;