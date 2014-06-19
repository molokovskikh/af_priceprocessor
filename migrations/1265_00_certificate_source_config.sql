alter table documents.CertificateFiles add column Note VARCHAR(255);
alter table documents.CertificateSources add column LookupUrl VARCHAR(255);
alter table documents.CertificateSources add column DecodeTableUrl VARCHAR(255);

alter table documents.CertificateSourceCatalogs add column Note VARCHAR(255);
