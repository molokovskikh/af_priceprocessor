alter table UserSettings.Defaults 
  add column ResponseSubjectMiniMailOnEmptyLetter VARCHAR(255) not null default 'Ваше Сообщение не доставлено одной или нескольким аптекам',
  add column ResponseBodyMiniMailOnEmptyLetter mediumtext;

update UserSettings.Defaults 
set
  ResponseBodyMiniMailOnEmptyLetter = 'Добрый день.\r\n\r\nВаше сообщение не будет доставлено аптекам в рамках системы АналитФармация, т.к.\r\nне установлены тема и тело письма.\r\n\r\nПожалуйста, заполните указанные поля и отправьте его вновь.\r\n\r\nВо вложении этого письма находится оригинал Вашего сообщения.\r\n\r\nС уважением, АК \"Инфорум\".'
where
  Id = 1;
