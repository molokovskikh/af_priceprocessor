alter table Documents.CertificateSources
  add column FtpFileDate datetime default null comment 'дата изменения файла, скаченного по ftp';

create table Documents.CertificateSourceCatalogs
(
  Id int unsigned not null auto_increment,
  CertificateSourceId int unsigned not null,
  CatalogId int unsigned default null,
  SerialNumber varchar(50) not null,
  SupplierCode varchar(50) not null,
  OriginFilePath varchar(255) not null,
  primary key (Id),
  KEY `IDX_CertificateCatalog` (CatalogId, SerialNumber),
  CONSTRAINT FK_CertificateSourceCatalogs_CertificateSourceId FOREIGN KEY (CertificateSourceId) REFERENCES Documents.CertificateSources (Id) ON DELETE cascade ON UPDATE cascade,
  CONSTRAINT FK_CertificateSourceCatalogs_CatalogId FOREIGN KEY (CatalogId) REFERENCES catalogs.catalog (Id) ON DELETE RESTRICT ON UPDATE RESTRICT
);
