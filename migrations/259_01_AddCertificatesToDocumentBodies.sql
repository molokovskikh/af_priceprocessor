alter table Documents.DocumentBodies
  add column CertificateId int unsigned default null,
  add column CertificateFilename varchar(255) default null comment 'имя файла образа сертификата',
  add column ProtocolFilemame varchar(255) default null comment 'имя файла образа протокола', 
  add column PassportFilename varchar(255) default null comment 'имя файла образа паспорта',
  add CONSTRAINT FK_DocumentBodies_CertificateId FOREIGN KEY (CertificateId) REFERENCES Documents.Certificates (Id) ON DELETE RESTRICT ON UPDATE RESTRICT;