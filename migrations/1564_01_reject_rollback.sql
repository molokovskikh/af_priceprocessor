alter table Documents.RejectHeaders drop foreign key FK_Documents_RejectHeaders_DownloadId;
alter table Documents.RejectHeaders drop foreign key FK_Documents_RejectHeaders_AddressId;
alter table Documents.RejectHeaders drop foreign key FK_Documents_RejectHeaders_SupplierId;
alter table Documents.RejectLines drop foreign key FK_Documents_RejectLines_HeaderId;
drop table if exists Documents.RejectLines;
drop table if exists Documents.RejectHeaders;
