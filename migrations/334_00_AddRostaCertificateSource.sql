insert into Documents.CertificateSources (SourceClassName) values ('RostaCertificateSource');

insert into documents.SourceSuppliers (CertificateSourceId, SupplierId)
SELECT 
cs.Id,
s.Id
FROM 
  (
  documents.CertificateSources cs,
  future.Suppliers s
  )
  left join documents.SourceSuppliers ss on s.Id = ss.SupplierId and ss.CertificateSourceId = cs.Id
where
    cs.SourceClassName = 'RostaCertificateSource'
and s.Segment = 0
and s.Name like 'Роста%'
and s.Disabled = 0
and ss.CertificateSourceId is null;
