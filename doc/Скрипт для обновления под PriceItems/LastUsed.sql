
set names cp1251;

CREATE TABLE  `farm`.`UsedSynonymLogs` (
  `SynonymCode` int(10) unsigned NOT NULL,
  `LastUsed` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`SynonymCode`) USING BTREE,
  CONSTRAINT `SynonymCode_FK` FOREIGN KEY `SynonymCode_IDX` (`SynonymCode`) REFERENCES `farm`.`synonym` (`SynonymCode`) ON DELETE CASCADE ON UPDATE CASCADE
);


CREATE TABLE  `farm`.`UsedSynonymFirmCrLogs` (
  `SynonymFirmCrCode` int(10) unsigned NOT NULL,
  `LastUsed` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`SynonymFirmCrCode`) USING BTREE,
  CONSTRAINT `SynonymFirmCrCode_FK` FOREIGN KEY `SynonymFirmCrCode_IDX` (`SynonymFirmCrCode`) REFERENCES `farm`.`synonymFirmCr` (`SynonymFirmCrCode`) ON DELETE CASCADE ON UPDATE CASCADE
);

drop trigger if exists farm.SynonymAfterInsert;

drop trigger if exists farm.SynonymFirmCrAfterInsert;

DELIMITER ;;

CREATE TRIGGER farm.SynonymAfterInsert AFTER Insert ON farm.Synonym FOR EACH ROW
BEGIN
  insert into farm.SynonymArchive(SynonymCode, PriceCode, Synonym, ProductId, Junk)
  values(NEW.SynonymCode, NEW.PriceCode, NEW.Synonym, NEW.ProductId, NEW.Junk);

  insert into farm.UsedSynonymLogs (SynonymCode) values (NEW.SynonymCode);

END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.SynonymFirmCrAfterInsert AFTER Insert ON farm.SynonymFirmCr FOR EACH ROW
BEGIN
  insert into farm.UsedSynonymFirmCrLogs (SynonymFirmCrCode) values (NEW.SynonymFirmCrCode);
END ;;


DELIMITER ;


insert into farm.UsedSynonymLogs (SynonymCode)
  select
    SynonymCode
  from
    farm.Synonym;

insert into farm.UsedSynonymFirmCrLogs (SynonymFirmCrCode)
  select
    SynonymFirmCrCode
  from
    farm.SynonymFirmCr;


alter table farm.Synonym
  drop column LastUsed;

alter table farm.SynonymFirmCr
  drop column LastUsed;