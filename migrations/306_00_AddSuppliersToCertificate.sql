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
    cs.SourceClassName = 'AptekaHoldingVoronezhCertificateSource'
and s.Segment = 0
and s.Name like 'Аптека-Холдинг%'
and ss.CertificateSourceId is null;


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
    cs.SourceClassName = 'ProtekSource'
and s.Segment = 0
and s.Name like 'Протек%'
and ss.CertificateSourceId is null;


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
    cs.SourceClassName = 'SiaSource'
and s.Segment = 0
and s.Name like 'СИА%'
and ss.CertificateSourceId is null;
