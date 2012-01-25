alter table documents.Mails 
  add column SHA256Hash VARCHAR(255) not null default '',
  add key IDX_Mails_SHA256Hash (SHA256Hash);
