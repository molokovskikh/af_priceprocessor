alter table documents.CertificateFiles add column Extension VARCHAR(255);

update Documents.CertificateFiles
set Extension = ".tif";


alter table documents.CertificateFiles change column Extension Extension VARCHAR(255) not null;
