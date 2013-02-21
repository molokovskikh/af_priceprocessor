alter table Documents.FileCertificates
add column Id int unsigned auto_increment not null,
add primary key(Id);

create temporary table duplicates engine=memory
select Id, CertificateId, CertificateFileId
from Documents.FileCertificates
group by CertificateId, CertificateFileId
having count(*) > 1;

create temporary table for_delete engine=memory
select f.Id
from Documents.FileCertificates f
	join duplicates d on d.CertificateId = f.CertificateId and d.CertificateFileId = f.CertificateFileId
where d.Id <> f.Id;

delete f
from Documents.FileCertificate f
	join for_delete d on d.Id = f.Id;

alter table Documents.FileCertificates
drop primary key,
drop column Id;


alter table Documents.FileCertificates
add primary key (CertificateId, CertificateFileId);
