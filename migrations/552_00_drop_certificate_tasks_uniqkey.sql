alter table Documents.CertificateTasks
drop foreign key FK_CertificateTasks_CertificateSourceId,
drop key IDX_CertificateTask;

alter table Documents.CertificateTasks
add CONSTRAINT `FK_CertificateTasks_CertificateSourceId` FOREIGN KEY (`CertificateSourceId`) REFERENCES `certificatesources` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE;
