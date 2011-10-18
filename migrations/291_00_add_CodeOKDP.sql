alter table Documents.DocumentBodies
  add column CodeOKDP varchar(20) default null comment 'код ОКДП' after `EAN13`;