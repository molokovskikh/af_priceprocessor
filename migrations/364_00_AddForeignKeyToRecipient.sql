alter table Logs.MailSendLogs
  add CONSTRAINT FK_MailSendLogs_RecipientId FOREIGN KEY (RecipientId) REFERENCES documents.mailrecipients(Id) ON DELETE set null;

