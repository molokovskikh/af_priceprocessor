insert into documents.SourceSuppliers (CertificateSourceId, SupplierId)
SELECT 
1,
s.Id
FROM 
  future.Suppliers s
  left join documents.SourceSuppliers ss on s.Id = ss.SupplierId 
where
  s.Segment = 0
and s.Name like 'Аптека-Холдинг%'
and ss.CertificateSourceId is null;


insert into documents.SourceSuppliers (CertificateSourceId, SupplierId)
SELECT 
4,
s.Id
FROM 
  future.Suppliers s
  left join documents.SourceSuppliers ss on s.Id = ss.SupplierId 
where
  s.Segment = 0
and s.Name like 'Протек%'
and ss.CertificateSourceId is null;


insert into documents.SourceSuppliers (CertificateSourceId, SupplierId)
SELECT 
2,
s.Id
FROM 
  future.Suppliers s
  left join documents.SourceSuppliers ss on s.Id = ss.SupplierId 
where
  s.Segment = 0
and s.Name like 'СИА%'
and ss.CertificateSourceId is null;
