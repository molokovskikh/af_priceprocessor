insert into Documents.CertificateSources (SourceClassName, FtpSupplierId) values ('FarmKomplektCertificateSource', 3414);

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
    cs.SourceClassName = 'FarmKomplektCertificateSource'
and s.Segment = 0
and s.Id in (3414, 4365, 78, 8085, 8115)
and ss.CertificateSourceId is null;
