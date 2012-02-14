alter table Usersettings.RetClientsSet 
  add column ProtekWaybillSavingType int not null default 0 comment 'формат для сохранения накладных Протека, полученных через сервис: 0 - sst, 1 - dbf';

update
  future.Clients,
  usersettings.Retclientsset
set
   Retclientsset.ProtekWaybillSavingType = 1
WHERE
  clients.Name LIKE '%ЛипецкФармация%'
  AND Retclientsset.ClientCode = clients.ID;

UPDATE future.Clients c,
  billing.Payers p,
  usersettings.Retclientsset
SET
  Retclientsset.ProtekWaybillSavingType = 1
WHERE
  p.PAYERID = c.PAYERID
  AND (c.Name LIKE '%Здоровые люди%'
  OR p.SHORTNAME LIKE '%Здоровые люди%')
  AND Retclientsset.ClientCode = c.ID;