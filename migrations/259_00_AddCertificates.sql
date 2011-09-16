create table Documents.Certificates(
  Id int unsigned not null auto_increment,
  CatalogId int unsigned not null,
  SerialNumber varchar(50) not null,
  primary key (Id),
  UNIQUE KEY `IDX_Certificate` (CatalogId, SerialNumber),
  CONSTRAINT FK_Certificates_CatalogId FOREIGN KEY (CatalogId) REFERENCES catalogs.catalog (Id) ON DELETE RESTRICT ON UPDATE RESTRICT
);

create table Documents.CertificateFiles(
  Id int unsigned not null auto_increment,
  CertificateId int unsigned not null,
  OriginFilename varchar(255) not null comment 'оригинальное имя сертификата',
  primary key (Id),
  CONSTRAINT FK_CertificateFiles_CertificateId FOREIGN KEY (CertificateId) REFERENCES Documents.Certificates (Id) ON DELETE RESTRICT ON UPDATE RESTRICT
);

create table Documents.CertificateTasks(
  Id int unsigned not null auto_increment,
  CatalogId int unsigned not null,
  SerialNumber varchar(50) not null,
  DocumentBodyId int unsigned not null,
  primary key (Id),
  UNIQUE KEY `IDX_CertificateTask` (CatalogId, SerialNumber),
  CONSTRAINT FK_CertificateTasks_CatalogId FOREIGN KEY (CatalogId) REFERENCES catalogs.catalog (Id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_CertificateTasks_DocumentBodyId FOREIGN KEY (DocumentBodyId) REFERENCES Documents.DocumentBodies (Id) ON DELETE RESTRICT ON UPDATE RESTRICT
);
