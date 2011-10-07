create table Documents.CertificateSources(
  Id int unsigned not null auto_increment,
  SourceSupplierId int unsigned not null,
  SourceClassName varchar(255) not null,
  primary key (Id),
  CONSTRAINT FK_CertificateSources_SourceSupplierId FOREIGN KEY (SourceSupplierId) REFERENCES future.Suppliers (Id) ON DELETE RESTRICT ON UPDATE RESTRICT
);


insert into  Documents.CertificateSources (Id, SourceSupplierId, SourceClassName) values (1, 39, 'AptekaHoldingVoronezhCertificateSource');


create table Documents.SourceSuppliers(
  CertificateSourceId int unsigned not null,
  SupplierId int unsigned not null,
  CONSTRAINT FK_SourceSuppliers_CertificateSourceId FOREIGN KEY (CertificateSourceId) REFERENCES Documents.CertificateSources (Id) ON DELETE cascade ON UPDATE cascade,
  CONSTRAINT FK_SourceSuppliers_SourceSupplierId FOREIGN KEY (SupplierId) REFERENCES future.Suppliers (Id) ON DELETE cascade ON UPDATE cascade
);

insert into  Documents.SourceSuppliers (CertificateSourceId, SupplierId) values (1, 39);


alter table Documents.CertificateTasks
  modify column SupplierId int unsigned default null;

alter table Documents.CertificateTasks
  add column CertificateSourceId int unsigned default null after SupplierId,
  add CONSTRAINT FK_CertificateTasks_CertificateSourceId FOREIGN KEY (CertificateSourceId) REFERENCES Documents.CertificateSources (Id) ON DELETE cascade ON UPDATE cascade;

update Documents.CertificateTasks
set
  CertificateSourceId = 1
where
  SupplierId = 39;



create table Documents.FileCertificates(
  CertificateId int unsigned not null,
  CertificateFileId int unsigned not null,
  CONSTRAINT FK_FileCertificates_CertificateId FOREIGN KEY (CertificateId) REFERENCES Documents.Certificates (Id) ON DELETE cascade ON UPDATE cascade,
  CONSTRAINT FK_FileCertificates_CertificateFileId FOREIGN KEY (CertificateFileId) REFERENCES Documents.CertificateFiles (Id) ON DELETE cascade ON UPDATE cascade
);


insert into Documents.FileCertificates (CertificateId, CertificateFileId)
select CertificateId, Id
from Documents.CertificateFiles;
