/*

Что нужно сделать и в какой последовательности
1. Обновить таблицу Core: добавать полей и выставить правильные значения PriceCode для мультифайловых колонок

2. В таблицу PricesCosts добавить поле PriceItemId

3. Сделать изменения с таблицами pricefmts, source_types, formrules, source.

4. Создать таблицу PriceItems, процедуры FillPriceItems и вызвать процедуру

5. Добавить в таблицы blockedPrice, forb, zero, UnrecExp поля PriceItemId

6. Добавить в таблицы logs.formlogs, logs.downlogs поля PriceItemId и заполнить их

7. Удалить цены из таблицы PricesData

8. Изменить поле PriceCode на ShowPriceCode в таблице PricesCost, и удалить поле ShowPriceCode

9. Удалить все остальные лишние поля

*/


-- Обновляем таблицу Core0

drop trigger if exists farm.Core0BeforeInsert;

alter table farm.Core0
  add column `JunkNew` tinyint(1) unsigned NOT NULL DEFAULT '0' after `Doc`,
  add column `AwaitNew` tinyint(1) unsigned NOT NULL DEFAULT '0' after `JunkNew`,
  drop column `BaseCost`;

update
  farm.Core0
set
  JunkNew =  if(length(Junk) > 0, 1, 0),
  AwaitNew = if(length(Await) > 0, 1, 0);

alter table farm.Core0
  drop key `Await_IDX`,
  drop key `Junk_IDX`,
  drop column `Junk`,
  drop colum `Await`;

alter table farm.Core0
  change column `JunkNew` `Junk` tinyint(1) unsigned NOT NULL DEFAULT '0',
  change column `AwaitNew` `Await` tinyint(1) unsigned NOT NULL DEFAULT '0',
  add key `Junk_IDX` (`Junk`) USING BTREE,
  add key `Await_IDX` (`Await`) USING BTREE;

drop table farm.distinctsynonymtmp;

drop table farm.distinctsynonymfirmcrtmp;


-- удаляем триггеры для работы с price_update_info

drop trigger if exists Catalogs.AssortmentAfterInsert;

drop trigger if exists farm.AssortmentAfterInsert;


-- добавляем к Synonym, SynonymFirmCr поля LastUsed
alter table farm.Synonym
  add column `LastUsed` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP;

alter table farm.SynonymFirmCr
  add column `LastUsed` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP;


-- корректируем таблицу usersettings.intersection
drop trigger if exists usersettings.IntersectionLogDelete;

drop trigger if exists usersettings.IntersectionLogInsert;

drop trigger if exists usersettings.IntersectionLogUpdate;

alter table usersettings.Intersection
  drop column `CostCorrOwner`,
  drop column `CostCorrByFirm`;

alter table logs.intersection_logs
  drop column `MaxSynonymFirmCrCode`,
  drop column `UncMaxSynonymFirmCrCode`,
  drop column `CalculateSynonymFirmCr`,
  drop column `MaxSynonymCode`,
  drop column `UncMaxSynonymCode`,
  drop column `CalculateSynonym`,
  drop column `ParentSynonymPriceCode`,
  drop column `UncommittedLastSent`,
  drop column `LastSent`,
  drop column `CalculateDate`,
  drop column `Calculate`,
  drop column `CostCorrOwner`,
  drop column `CostCorrByFirm`;


DELIMITER ;;

CREATE TRIGGER UserSettings.IntersectionLogInsert AFTER Insert ON UserSettings.Intersection FOR EACH ROW
BEGIN
        INSERT 
        INTO    `logs`.intersection_logs 
                SET LogTime = now()
                ,
                OperatorName = IFNULL(
                        @INUser, 
                        SUBSTRING_INDEX(USER(),'@',1)
                ) 
                ,
                OperatorHost = IFNULL(
                        @INHost, 
                        SUBSTRING_INDEX(USER(),'@',-1)
                ) 
                , 
                Operation               = 0, 
                IntersectionID          = NEW.Id ,
                ClientCode              = NEW.ClientCode ,
                RegionCode              = NEW.RegionCode ,
                DisabledByAgency        = NEW.DisabledByAgency ,
                AlowDisabledByClient    = NEW.AlowDisabledByClient ,
                DisabledByClient        = NEW.DisabledByClient ,
                InvisibleOnFirm         = NEW.InvisibleOnFirm ,
                InvisibleOnClient       = NEW.InvisibleOnClient ,
                PriceCode               = NEW.PriceCode ,
                CostCode                = NEW.CostCode ,
                MinReq                  = NEW.MinReq ,
                PublicCostCorr          = NEW.PublicCostCorr ,
                FirmCostCorr            = NEW.FirmCostCorr ,
                CostCorrByClient        = NEW.CostCorrByClient ;
END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER UserSettings.IntersectionLogUpdate AFTER Update ON UserSettings.Intersection FOR EACH ROW
BEGIN

if (NEW.ClientCode <> OLD.ClientCode) or
(NEW.RegionCode <> OLD.RegionCode) or
(NEW.DisabledByAgency <> OLD.DisabledByAgency) or
(NEW.AlowDisabledByClient <> OLD.AlowDisabledByClient) or
(NEW.DisabledByClient <> OLD.DisabledByClient) or
(NEW.InvisibleOnFirm <> OLD.InvisibleOnFirm) or
(NEW.InvisibleOnClient <> OLD.InvisibleOnClient) or
(NEW.PriceCode <> OLD.PriceCode) or
(NEW.CostCode <> OLD.CostCode) or
(NEW.FirmClientCode <> OLD.FirmClientCode) or
(NEW.FirmClientCode2 <> OLD.FirmClientCode2) or
(NEW.FirmClientCode3 <> OLD.FirmClientCode3) or
(NEW.MinReq <> OLD.MinReq) or
(NEW.ControlMinReq <> OLD.ControlMinReq) or
(NEW.FirmCategory <> OLD.FirmCategory) or
(NEW.PublicCostCorr <> OLD.PublicCostCorr) or
(NEW.FirmCostCorr <> OLD.FirmCostCorr) or
(NEW.CostCorrByClient <> OLD.CostCorrByClient)
then
INSERT
        INTO    `logs`.intersection_logs
                SET LogTime = now()
                ,
                OperatorName = IFNULL(
                        @INUser,
                        SUBSTRING_INDEX(USER(),'@',1)
                ) 
                , 
                OperatorHost = IFNULL(
                        @INHost,
                        SUBSTRING_INDEX(USER(),'@',-1)
                )
                , 
                Operation      = 1, 
                IntersectionID = OLD.Id ,
                ClientCode     = IFNULL(
                         NEW.ClientCode,
                        OLD.ClientCode
                ) 
                ,
                RegionCode = IFNULL(
                        NEW.RegionCode,
                        OLD.RegionCode
                ) 
                ,
                DisabledByAgency = NULLIF(
                        NEW.DisabledByAgency,
                        OLD.DisabledByAgency
                ) 
                ,
                AlowDisabledByClient = NULLIF(
                        NEW.AlowDisabledByClient,
                        OLD.AlowDisabledByClient
                ) 
                ,
                DisabledByClient = NULLIF(
                        NEW.DisabledByClient,
                        OLD.DisabledByClient
                )
                ,
                InvisibleOnFirm = NULLIF(
                        NEW.InvisibleOnFirm,
                        OLD.InvisibleOnFirm
                )
                ,
                InvisibleOnClient = NULLIF(
                        NEW.InvisibleOnClient,
                        OLD.InvisibleOnClient
                )
                ,
                PriceCode = IFNULL(
                        NEW.PriceCode,
                        OLD.PriceCode
                )
                ,
                CostCode = NULLIF(
                        NEW.CostCode,
                        OLD.CostCode
                )
                ,
                FirmClientCode = NULLIF(
                        NEW.FirmClientCode,
                        OLD.FirmClientCode
                )
                ,
                FirmClientCode2 = NULLIF(
                        NEW.FirmClientCode2,
                        OLD.FirmClientCode2
                )
                ,
                FirmClientCode3 = NULLIF(
                        NEW.FirmClientCode3,
                        OLD.FirmClientCode3
                )
                ,
                MinReq = NULLIF(
                        NEW.MinReq,
                        OLD.MinReq
                )
                ,
                ControlMinReq = NULLIF(
                        NEW.ControlMinReq,
                        OLD.ControlMinReq
                )
                ,
                FirmCategory = NULLIF(
                        NEW.FirmCategory,
                        OLD.FirmCategory
                )
                ,
                PublicCostCorr = NULLIF(
                        NEW.PublicCostCorr,
                        OLD.PublicCostCorr
                )
                ,
                FirmCostCorr = NULLIF(
                        NEW.FirmCostCorr,
                        OLD.FirmCostCorr
                )
                ,
                CostCorrByClient = NULLIF(
                        NEW.CostCorrByClient,
                        OLD.CostCorrByClient
                );
 end if;
 if (NEW.CostCode <> OLD.CostCode) or
  (NEW.PublicCostCorr <> OLD.PublicCostCorr) or
  (NEW.FirmCostCorr <> OLD.FirmCostCorr) or
  (NEW.DisabledByClient <> OLD.DisabledByClient) or
  (NEW.DisabledByAgency <> OLD.DisabledByAgency) or
  (NEW.InvisibleOnClient <> OLD.InvisibleOnClient)
  then
UPDATE intersection_update_info
        SET lastsent  ='2003-01-01 00:00:00',
        UncommittedLastSent='2003-01-01 00:00:00'
WHERE   pricecode     =OLD.PriceCode 
        AND clientcode=OLD.ClientCode 
        AND regioncode=OLD.RegionCode;
        end if;
END ;;

DELIMITER ;

DELIMITER ;;

CREATE TRIGGER UserSettings.IntersectionLogDelete AFTER Delete ON UserSettings.Intersection FOR EACH ROW
BEGIN

INSERT INTO `logs`.intersection_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
IntersectionID = OLD.Id
,ClientCode = OLD.ClientCode
,RegionCode = OLD.RegionCode
,DisabledByAgency = OLD.DisabledByAgency
,DisabledByFirm = OLD.DisabledByFirm
,AlowDisabledByClient = OLD.AlowDisabledByClient
,DisabledByClient = OLD.DisabledByClient
,InvisibleOnFirm = OLD.InvisibleOnFirm
,InvisibleOnClient = OLD.InvisibleOnClient
,PriceCode = OLD.PriceCode
,CostCode = OLD.CostCode
,FirmClientCode = OLD.FirmClientCode
,FirmClientCode2 = OLD.FirmClientCode2
,FirmClientCode3 = OLD.FirmClientCode3
,MinReq = OLD.MinReq
,ControlMinReq = OLD.ControlMinReq
,FirmCategory = OLD.FirmCategory
,PublicCostCorr = OLD.PublicCostCorr
,FirmCostCorr = OLD.FirmCostCorr
,CostCorrByClient = OLD.CostCorrByClient;
END ;;

DELIMITER ;


-- обновляем таблицу CostFormRules
-- потом не забыть восстановить логирование CostFormRules
drop trigger if exists farm.CostFormRulesLogDelete;

drop trigger if exists farm.CostFormRulesLogInsert;

drop trigger if exists farm.CostFormRulesLogUpdate;

alter table farm.CostFormRules
  drop primary key,
  drop key `PC_CostCode_IDX`,
  drop foreign key `CostFormRules_ibfk_1`,
  drop column `FR_Id`,
  change column `PC_CostCode` `CostCode` int(11) unsigned NOT NULL;

delete from farm.CostFormRules
where
  not exists(select * from usersettings.pricescosts pc where pc.CostCode = CostFormRules.CostCode);

alter table farm.CostFormRules
  add primary key(`CostCode`),
  add constraint `CostCode_FK` foreign key (`CostCode`) REFERENCES `usersettings`.`pricescosts` (`CostCode`) ON DELETE cascade ON UPDATE CASCADE;


-- обновляем таблицу PricesRetrans
-- потом не забыть восстановить логирование PricesRetrans
drop trigger if exists logs.PricesRetransAfterInsert;

alter table logs.PricesRetrans
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `OperatorHost`;

-- обновляем таблицу PricesCosts
-- потом не забыть восстановить логирование PricesCosts
drop trigger if exists usersettings.pricescostsLogDelete;

drop trigger if exists usersettings.pricescostsLogInsert;

drop trigger if exists usersettings.pricescostsLogUpdate;

alter table usersettings.PricesCosts
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `PriceCode`;

alter table logs.prices_costs_logs
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `PriceCode`,
  drop column `ShowPriceCode`;


-- обновляем таблицу PricesData
-- потом не забыть восстановить логирование PricesData
drop trigger if exists usersettings.pricesdataLogDelete;

drop trigger if exists usersettings.pricesdataLogInsert;

drop trigger if exists usersettings.pricesdataLogUpdate;

alter table logs.prices_data_logs
  drop column WaitingDownloadInterval;


-- обновляем таблицу PricesFmts
alter table farm.`pricefmts`
  change column `Format` `Format` char(20) NOT NULL DEFAULT '',
  change column `Comment` `Comment` varchar(255) DEFAULT NULL,
  add column `ParserClassName` varchar(255) DEFAULT NULL;


-- обновляем таблицу FormRules
-- потом не забыть восстановить логирование formrules
drop trigger if exists farm.UpdatePriceCode;

update
  farm.formrules
set
  delimiter = null
where
  formrules.delimiter = ''
and (formrules.PriceFmt in ('DOS', 'WIN'));


-- заполяем таблицу PriceFmts
delete from farm.`pricefmts`;

insert into farm.`pricefmts`
  (Id, `Format`, `FileExtention`, `Comment`, `ParserClassName`)
  values
  (1, 'DelimWIN', '.txt', 'Текстовый файл с разделителями кодировки Windows', 'TXTWinDelimiterPriceParser'),
  (2, 'DelimDOS', '.txt', 'Текстовый файл с разделителями кодировки DOS', 'TXTDosDelimiterPriceParser'),
  (3, 'XLS', '.xls', 'Excel-файл', 'ExcelPriceParser'),
  (4, 'DBF', '.dbf', 'DBF-файл', 'DBFPriceParser'),
  (5, 'XML', '.xml', 'CommerceML-файл', 'CommerceMLParser'),
  (6, 'FixedWIN', '.txt', 'Текстовый файл с фиксированной шириной колонок кодировки Windows', 'TXTWinFixedPriceParser'),
  (7, 'FixedDOS', '.txt', 'Текстовый файл с фиксированной шириной колонок кодировки DOS', 'TXTDosFixedPriceParser');


alter table farm.formrules
  add column `Id` int(11) unsigned NOT NULL first,
  add column `PriceFormatId` int(11) unsigned DEFAULT NULL after `Id`,
  drop column `Segment`,
  drop column `Flag`;

alter table logs.form_rules_logs
  add column `PriceFormatId` int(11) unsigned DEFAULT NULL after `FormRulesId`,
  drop column `FirmName`,
  drop column `FullName`,
  drop column `FirmURL`,
  drop column `Town`,
  drop column `FirmCr`,
  drop column `CountryCr`,
  drop column `FormPost`,
  drop column `DateLastForm`,
  drop column `DateCurPrice`,
  drop column `DatePrevPrice`,
  drop column `PayDate`,
  drop column `Segment`,
  drop column `Flag`,
  drop column `PriceFmt`,
  drop column `PriceFile`,
  drop column `PriceBLOB`,
  drop column `TxtAsFactCostBegin`,
  drop column `TxtAsFactCostEnd`,
  drop column `Txt5DayCostBegin`,
  drop column `Txt5DayCostEnd`,
  drop column `Txt10DayCostBegin`,
  drop column `Txt10DayCostEnd`,
  drop column `Txt15DayCostBegin`,
  drop column `Txt15DayCostEnd`,
  drop column `Txt20DayCostBegin`,
  drop column `Txt20DayCostEnd`,
  drop column `Txt25DayCostBegin`,
  drop column `Txt25DayCostEnd`,
  drop column `Txt30DayCostBegin`,
  drop column `Txt30DayCostEnd`,
  drop column `Txt45DayCostBegin`,
  drop column `Txt45DayCostEnd`,
  drop column `TxtReserved3Begin`,
  drop column `TxtReserved3End`,
  drop column `TxtUpCostBegin`,
  drop column `TxtUpCostEnd`,
  drop column `FAsFactCost`,
  drop column `F5DayCost`,
  drop column `F10DayCost`,
  drop column `F15DayCost`,
  drop column `F20DayCost`,
  drop column `F25DayCost`,
  drop column `F30DayCost`,
  drop column `F45DayCost`,
  drop column `FUpCost`,
  drop column `FReserved3`,
  drop column `AddField`,
  drop column `Symbol1`,
  drop column `Symbol2`,
  drop column `Symbol3`,
  drop column `Symbol4`,
  drop column `NoteText1`,
  drop column `NoteText2`,
  drop column `NoteText3`,
  drop column `NoteText4`,
  drop column `Tag1`,
  drop column `Tag2`,
  drop column `Tag3`,
  drop column `PosNum`,
  drop column `FirmCurs`,
  drop column `MMVBPlus`,
  drop column `RegionMask`,
  drop column `Addition`,
  drop column `AutoEMail`,
  add column `FOrderCost` varchar(20) default NULL,
  add column `TxtOrderCostBegin` int(11) unsigned default NULL,
  add column `TxtOrderCostEnd` int(11) unsigned default NULL,
  add column `FMinOrderCount` varchar(20) default NULL,
  add column `TxtMinOrderCountBegin` int(11) unsigned default NULL,
  add column `TxtMinOrderCountEnd` int(11) unsigned default NULL;


update farm.formrules
set
  Id = FirmCode;

update
  farm.formrules
set
  Id = PriceCode,
  PriceFormatId = 
    if(PriceFMT = 'XLS', 3, 
       if(PriceFMT = 'DBF', 4, 
          if(PriceFMT = 'XML', 5, 
             if(PriceFMT = 'WIN' and Delimiter is not null, 1,
                if(PriceFMT = 'DOS' and Delimiter is not null, 2,
                   if(PriceFMT = 'WIN' and Delimiter is null, 6, 
                      if(PriceFMT = 'DOS' and Delimiter is null, 7, null)
                   )
                )
             )
          )
       )
    );


-- обновляем таблицу SourceTypes
alter table farm.sourcetypes
  add column `Id` int(11) unsigned NOT NULL DEFAULT '0' first;

update farm.sourcetypes set Id = 1 where Type = 'EMail';
update farm.sourcetypes set Id = 2 where Type = 'HTTP';
update farm.sourcetypes set Id = 3 where Type = 'FTP';
update farm.sourcetypes set Id = 4 where Type = 'LAN';

alter table farm.sourcetypes
  add primary key (`Id`);


-- обновляем таблицу Sources
-- потом не забыть восстановить логирование sources
drop trigger if exists farm.sourcesLogDelete;

drop trigger if exists farm.sourcesLogInsert;

drop trigger if exists farm.sourcesLogUpdate;

alter table farm.sources
  add column `Id` int(11) unsigned NOT NULL first,
  add column `SourceTypeId` int(11) unsigned DEFAULT NULL after `Id`;

update
  farm.sources
set
  sources.Id = sources.FirmCode;

update
  farm.sources,
  farm.sourcetypes
set
  sources.SourceTypeId = sourcetypes.Id
where
  sourcetypes.Type = sources.SourceType;


-- обновляем таблицы BlockedPrice, Forb, Zero, UnrecExp
alter table farm.`blockedPrice`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL first;

alter table farm.`forb`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `RowId`;

alter table farm.`zero`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `RowId`;

alter table farm.`UnrecExp`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `RowId`,
  drop column BlockBy;


-- обновляем таблицу FormLogs
alter table logs.formlogs
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `LogTime`,
  add column `Host` varchar(50) DEFAULT NULL after `PriceItemId`;

update logs.formlogs
set
  Host = if(AppCode = 7, 'fms', 'prg1');

-- обновляем таблицу DownLogs
alter table logs.downlogs
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `LogTime`,
  add column `Host` varchar(50) DEFAULT NULL after `PriceItemId`;

update logs.downlogs
set
  Host = if(AppCode = 3, 'fms', 'prg1');


CREATE TABLE usersettings.`PriceItems` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `FormRuleId` int(10) unsigned NOT NULL,
  `SourceId` int(10) unsigned NOT NULL,
  `RowCount` int(10) unsigned DEFAULT NULL COMMENT 'Количество позиций в \r\nформализованном прайс-листе',
  `UnformCount` int(10) unsigned DEFAULT NULL COMMENT 'Количество \r\nнеформализованных позиций в прайсе.',
  `PriceDate` datetime DEFAULT NULL,
  `LastFormalization` datetime DEFAULT NULL,
  `LastRetrans` datetime DEFAULT NULL COMMENT 'Дата последнего перепроведения \r\nпрайс-листа обработкой.',
  `LastSynonymsCreation` datetime DEFAULT NULL COMMENT 'Дата последнего \r\nсоздания синонимов наименований или производителей обработкой.',
  `WaitingDownloadInterval` int(11) unsigned NOT NULL DEFAULT '1' COMMENT 'интервал (в часах), во время которого должен прийти свежий прайс-лист',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;


DELIMITER ;;

CREATE PROCEDURE usersettings.`FillPriceItems`()
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE PriceCode, CostCode INT Unsigned;
  DECLARE PriceRowCount, PriceUnformCount, CostRowCount, CostUnformCount, PriceWaitingDownloadInterval INT Unsigned;
  Declare PriceDate, PriceLastRetrans, PriceLastSynonymsCreation, CostDate, CostLastRetrans, CostLastSynonymsCreation, PriceLastFormalization, CostLastFormalization datetime;
  DECLARE CostType, BaseCost Tinyint;
  declare LastPriceItemId Int unsigned;

  DECLARE Prices CURSOR FOR select
p.PriceCode,
p.CostType,
pc.CostCode,
pc.basecost,
pui.RowCount,
pui.UnformCount,
pui.LastRetrans,
pui.LastSynonymsCreation,
pui.DateCurPrice,
pui.DateLastForm,
cui.RowCount,
cui.UnformCount,
cui.LastRetrans,
cui.LastSynonymsCreation,
cui.DateCurPrice,
cui.DateLastForm,
p.WaitingDownloadInterval
from
(
usersettings.pricesdata p,
usersettings.pricescosts pc,
usersettings.price_update_info pui
)
left join usersettings.price_update_info cui on cui.PRICECODE = pc.PriceCode
where
    pc.SHOWPRICECODE = p.PriceCode
and ( ( ((p.COSTTYPE = 0) or (p.CostType is null)) and (pc.BaseCost = 1) ) or (p.CostType = 1) )
and pui.PRICECODE = p.PriceCode
;
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  OPEN Prices;

  REPEAT
    FETCH Prices INTO PriceCode, CostType, CostCode, BaseCost,
          PriceRowCount, PriceUnformCount, PriceLastRetrans, PriceLastSynonymsCreation, PriceDate, PriceLastFormalization,
          CostRowCount, CostUnformCount, CostLastRetrans, CostLastSynonymsCreation, CostDate, CostLastFormalization PriceWaitingDownloadInterval;
    IF NOT done THEN

      -- Вставка
			if ((CostType = 0) or (CostType is null))
			then
        insert into usersettings.PriceItems
          (FormRuleId, SourceId, RowCount, UnformCount, PriceDate, LastRetrans, LastSynonymsCreation, WaitingDownloadInterval, LastFormalization)
          values
          (PriceCode, PriceCode, PriceRowCount, PriceUnformCount, PriceDate, PriceLastRetrans, PriceLastSynonymsCreation, PriceWaitingDownloadInterval, PriceLastFormalization);
      else
        insert into usersettings.PriceItems
          (FormRuleId, SourceId, RowCount, UnformCount, PriceDate, LastRetrans, LastSynonymsCreation, WaitingDownloadInterval, LastFormalization)
          values
          (CostCode, CostCode, CostRowCount, CostUnformCount, CostDate, CostLastRetrans, CostLastSynonymsCreation, PriceWaitingDownloadInterval, CostLastFormalization);
      end if;
      select last_insert_id() into LastPriceItemId;
      update usersettings.pricescosts set PriceItemId = LastPriceItemId where pricescosts.CostCode = CostCode;

    END IF;
  UNTIL done END REPEAT;

  CLOSE Prices;

END ;;

DELIMITER ;

call usersettings.FillPriceItems;


alTER TABLE `usersettings`.`price_update_info` 
  RENAME TO `usersettings`.`price_update_info_OLD`;

-- обновляем все таблицы взяв эти значения из PriceItems

-- обновления Core0
update
  farm.Core0,
  usersettings.PricesCosts
set
  Core0.PriceCode = PricesCosts.ShowPriceCode
where
    Core0.PriceCode = PricesCosts.PriceCode
and PricesCosts.ShowPriceCode != PricesCosts.PriceCode;


-- обновления blockedPrice
update
  farm.blockedPrice,
  usersettings.PricesCosts
set
  blockedPrice.PriceItemId = PricesCosts.PriceItemId
where
  blockedPrice.PriceCode = PricesCosts.PriceCode;

update
  farm.blockedPrice,
  usersettings.PricesCosts
set
  blockedPrice.PriceItemId = PricesCosts.PriceItemId
where
    blockedPrice.PriceCode = PricesCosts.ShowPriceCode
and blockedPrice.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.blockedPrice,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  blockedPrice.PriceItemId = parent.PriceItemId
where
    blockedPrice.PriceCode = PricesCosts.PriceCode
and blockedPrice.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.blockedPrice
where
  blockedPrice.PriceItemId is null;

alter table farm.blockedPrice
  drop primary key,
  drop column `PriceCode`,
  add primary key (`PriceItemId`);


-- обновления forb
update
  farm.forb,
  usersettings.PricesCosts
set
  forb.PriceItemId = PricesCosts.PriceItemId
where
  forb.PriceCode = PricesCosts.PriceCode;

update
  farm.forb,
  usersettings.PricesCosts
set
  forb.PriceItemId = PricesCosts.PriceItemId
where
    forb.PriceCode = PricesCosts.ShowPriceCode
and forb.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.forb,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  forb.PriceItemId = parent.PriceItemId
where
    forb.PriceCode = PricesCosts.PriceCode
and forb.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.forb
where
  forb.PriceItemId is null;

alter table farm.forb
  drop key `PriceCode_IDX`,
  drop column `PriceCode`,
  add key `PriceItemId_IDX` using BTree (`PriceItemId`);


-- обновления zero
update
  farm.zero,
  usersettings.PricesCosts
set
  zero.PriceItemId = PricesCosts.PriceItemId
where
  zero.PriceCode = PricesCosts.PriceCode;

update
  farm.zero,
  usersettings.PricesCosts
set
  zero.PriceItemId = PricesCosts.PriceItemId
where
    zero.PriceCode = PricesCosts.ShowPriceCode
and zero.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.zero,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  zero.PriceItemId = parent.PriceItemId
where
    zero.PriceCode = PricesCosts.PriceCode
and zero.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.zero
where
  zero.PriceItemId is null;

alter table farm.zero
  drop key `PriceCode_IDX`,
  drop column `PriceCode`,
  add key `PriceItemId_IDX` using BTree (`PriceItemId`);

-- обновления unrecexp
update
  farm.unrecexp,
  usersettings.PricesCosts
set
  unrecexp.PriceItemId = PricesCosts.PriceItemId
where
  unrecexp.PriceCode = PricesCosts.PriceCode;

update
  farm.unrecexp,
  usersettings.PricesCosts
set
  unrecexp.PriceItemId = PricesCosts.PriceItemId
where
    unrecexp.PriceCode = PricesCosts.ShowPriceCode
and unrecexp.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.unrecexp,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  unrecexp.PriceItemId = parent.PriceItemId
where
    unrecexp.PriceCode = PricesCosts.PriceCode
and unrecexp.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.unrecexp
where
  unrecexp.PriceItemId is null;

alter table farm.unrecexp
  drop key `PriceCode_IDX`,
  drop column `PriceCode`,
  drop column `BlockBy`,
  add key `PriceItemId_IDX` using BTree (`PriceItemId`);


-- обновления PricesRetrans
update
  logs.PricesRetrans,
  usersettings.PricesCosts
set
  PricesRetrans.PriceItemId = PricesCosts.PriceItemId
where
  PricesRetrans.PriceCode = PricesCosts.PriceCode;

update
  logs.PricesRetrans,
  usersettings.PricesCosts
set
  PricesRetrans.PriceItemId = PricesCosts.PriceItemId
where
    PricesRetrans.PriceCode = PricesCosts.ShowPriceCode
and PricesRetrans.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  logs.PricesRetrans,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  PricesRetrans.PriceItemId = parent.PriceItemId
where
    PricesRetrans.PriceCode = PricesCosts.PriceCode
and PricesRetrans.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  logs.PricesRetrans
where
  PricesRetrans.PriceItemId is null;

alter table logs.PricesRetrans
  drop key `PriceCode`,
  drop key `LogTime`,
  drop column `PriceCode`,
  add key `PriceItemId_IDX` (`PriceItemId`),
  add key `LogTime_IDX` (`LogTime`, `PriceItemId`);


-- обновления formlogs
update
  logs.formlogs,
  usersettings.PricesCosts
set
  formlogs.PriceItemId = PricesCosts.PriceItemId
where
  formlogs.PriceCode = PricesCosts.PriceCode;

update
  logs.formlogs,
  usersettings.PricesCosts
set
  formlogs.PriceItemId = PricesCosts.PriceItemId
where
    formlogs.PriceCode = PricesCosts.ShowPriceCode
and formlogs.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  logs.formlogs,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  formlogs.PriceItemId = parent.PriceItemId
where
    formlogs.PriceCode = PricesCosts.PriceCode
and formlogs.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  logs.formlogs
where
  formlogs.PriceItemId is null;

alter table logs.formlogs
  drop primary key,
  drop key `PriceCode`,
  drop key `AppCode`,
  drop column `PriceCode`,
  drop column `AppCode`,
  add primary key (`RowId`),
  add key `PriceItemId_IDX` (`RowId`, `PriceItemId`),
  add key `Host_IDX` (`Host`);



-- обновления downlogs
update
  logs.downlogs,
  usersettings.PricesCosts
set
  downlogs.PriceItemId = PricesCosts.PriceItemId
where
  downlogs.PriceCode = PricesCosts.PriceCode;

update
  logs.downlogs,
  usersettings.PricesCosts
set
  downlogs.PriceItemId = PricesCosts.PriceItemId
where
    downlogs.PriceCode = PricesCosts.ShowPriceCode
and downlogs.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  logs.downlogs,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  downlogs.PriceItemId = parent.PriceItemId
where
    downlogs.PriceCode = PricesCosts.PriceCode
and downlogs.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  logs.downlogs
where
    downlogs.PriceItemId is null
and downlogs.PriceCode != 0;

alter table logs.downlogs
  drop primary key,
  drop key `PriceCode`,
  drop column `PriceCode`,
  drop column `AppCode`,
  add primary key (`RowId`),
  add key `PriceItemId_IDX` (`RowId`, `PriceItemId`),
  add key `Host_IDX` (`Host`);


-- обновляем таблицу sources
alter table farm.sources
  drop foreign key `sources_ibfk_1`,
  drop primary key,
  drop column `FirmCode`,
  change column `Id` `Id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  add primary key (`Id`),
  drop column `SourceType`,
  drop column `LastDateTime`,
  drop column `PriceDateTime`,
  add constraint `SourceTypeId_FK` foreign key `SourceTypeId_IDX` (`SourceTypeId`) REFERENCES `farm`.`sourcetypes` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE; 
  

-- обновляем таблицу formrules
alter table farm.formrules
  drop primary key,
  drop key `FirmCode_IDX`,
  drop key `ParentSynonym_IDX`,
  drop key `ParentFormRules_IDX`,
  drop foreign key `formrules_ibfk_1`,
  drop foreign key `formrules_ibfk_2`,
  drop foreign key `formrules_ibfk_3`,
  change column `Id` `Id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  drop column `FirmCode`,
  drop column `PriceCode`,
  drop column `PriceFmt`,
  add primary key(`Id`),
  add constraint `PriceFormatId_FK` foreign key `PriceFormatId_IDX` (`PriceFormatId`) REFERENCES `farm`.`pricefmts` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE,
  add constraint `ParentFormRules_FK` foreign key `ParentFormRules_IDX` (`ParentFormRules`) REFERENCES `farm`.`formrules` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE,
  add constraint `ParentSynonym_FK` foreign key `ParentSynonym_IDX` (`ParentSynonym`) REFERENCES `usersettings`.`pricesdata` (`PriceCode`) ON DELETE SET NULL ON UPDATE CASCADE;



-- удаляем цены из PricesData и реструктуризации PricesCosts
alter table usersettings.PricesCosts
  drop foreign key `pricescosts_ibfk_1`;

delete from usersettings.PricesData
where
  not exists(select * from usersettings.pricescosts pc where pc.PriceCode = PricesData.PriceCode);

delete from usersettings.PricesData
using 
  usersettings.PricesData,
  usersettings.PricesCosts
where
    PricesData.PriceCode = PricesCosts.PriceCode
and PricesCosts.PriceCode != PricesCosts.ShowPriceCode;

delete from usersettings.PricesCosts
where
  not exists(select * from usersettings.PricesData where PricesData.PriceCode = PricesCosts.ShowPriceCode);

update
  usersettings.PricesCosts
set
  PriceCode = ShowPriceCode;

alter table usersettings.PricesCosts
  drop key `ShowPriceCode_IDX`,
  drop column `ShowPriceCode`,
  add constraint `PriceCode_FK` foreign key (`PriceCode`) REFERENCES `usersettings`.`pricesdata` (`PriceCode`) ON DELETE CASCADE ON UPDATE CASCADE;

alter table usersettings.PricesData
  drop column `WaitingDownloadInterval`;

update 
  usersettings.PricesCosts pc,
  usersettings.PricesCosts BasePC
set
  pc.PriceItemId = BasePC.PriceItemId
where
    pc.PriceItemId is null
and pc.BaseCost = 0
and BasePC.PriceCode = pc.PriceCode
and BasePC.BaseCost = 1;

-- везде восстанавливаем логирование

-- восстанавливаем CostFormRules
alter table logs.cost_form_rules_logs
  drop column `FR_Id`,
  change column `PC_CostCode` `CostCode` int(11) unsigned DEFAULT NULL;

DELIMITER ;;

CREATE TRIGGER farm.CostFormRulesLogInsert AFTER INSERT
ON farm.costformrules FOR EACH ROW BEGIN
INSERT INTO `logs`.cost_form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
CostCode = NEW.CostCode
,FieldName = NEW.FieldName
,TxtBegin = NEW.TxtBegin
,TxtEnd = NEW.TxtEnd
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.CostFormRulesLogDelete AFTER DELETE
ON farm.costformrules FOR EACH ROW BEGIN
INSERT INTO `logs`.cost_form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
CostCode = OLD.CostCode
,FieldName = OLD.FieldName
,TxtBegin = OLD.TxtBegin
,TxtEnd = OLD.TxtEnd
;END ;;

DELIMITER ;


DELIMITER ;;


CREATE TRIGGER farm.CostFormRulesLogUpdate AFTER UPDATE
ON farm.costformrules FOR EACH ROW BEGIN
INSERT INTO `logs`.cost_form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
CostCode = IFNULL(NEW.CostCode, OLD.CostCode)
,FieldName = NULLIF(NEW.FieldName, OLD.FieldName)
,TxtBegin = NULLIF(NEW.TxtBegin, OLD.TxtBegin)
,TxtEnd = NULLIF(NEW.TxtEnd, OLD.TxtEnd)
;END ;;

DELIMITER ;


-- восстанавливаем PricesRetrans

DELIMITER ;;

CREATE TRIGGER logs.PricesRetransAfterInsert AFTER INSERT
ON logs.PricesRetrans FOR EACH ROW BEGIN
  update usersettings.PriceItems
  set
    LastRetrans = NEW.LogTime
  where
    PriceItemId = NEW.PriceItemId;
END ;;

DELIMITER ;



-- восстанавливаем PricesCosts

DELIMITER ;;

CREATE TRIGGER usersettings.pricescostsLogInsert AFTER INSERT
ON usersettings.pricescosts FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_costs_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
pricescostsID = NEW.CostCode
,PriceCode = NEW.PriceCode
,PriceItemId = NEW.PriceItemId
,Enabled = NEW.Enabled
,AgencyEnabled = NEW.AgencyEnabled
,CostName = NEW.CostName
,BaseCost = NEW.BaseCost
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricescostsLogUpdate AFTER UPDATE
ON usersettings.pricescosts FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_costs_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
pricescostsID = OLD.CostCode
,PriceCode = IFNULL(NEW.PriceCode, OLD.PriceCode)
,PriceItemId = IFNULL(NEW.PriceItemId, OLD.PriceItemId)
,Enabled = NULLIF(NEW.Enabled, OLD.Enabled)
,AgencyEnabled = NULLIF(NEW.AgencyEnabled, OLD.AgencyEnabled)
,CostName = NULLIF(NEW.CostName, OLD.CostName)
,BaseCost = NULLIF(NEW.BaseCost, OLD.BaseCost)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricescostsLogDelete AFTER DELETE
ON usersettings.pricescosts FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_costs_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
pricescostsID = OLD.CostCode
,PriceCode = OLD.PriceCode
,PriceItemId = OLD.PriceItemId
,Enabled = OLD.Enabled
,AgencyEnabled = OLD.AgencyEnabled
,CostName = OLD.CostName
,BaseCost = OLD.BaseCost
;END ;;

DELIMITER ;


-- восстанавливаем PricesData

DELIMITER ;;

CREATE TRIGGER usersettings.pricesdataLogInsert AFTER INSERT
ON usersettings.pricesdata FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_data_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
pricesdataID = NEW.PriceCode
,FirmCode = NEW.FirmCode
,RegionMask = NEW.RegionMask
,AgencyEnabled = NEW.AgencyEnabled
,Enabled = NEW.Enabled
,ShowInWeb = NEW.ShowInWeb
,AlowInt = NEW.AlowInt
,PriceType = NEW.PriceType
,PriceName = NEW.PriceName
,MinReq = NEW.MinReq
,upcost = NEW.upcost
,priceinfo = NEW.priceinfo
,percentcorr = NEW.percentcorr
,OrderEmailSubject = NEW.OrderEmailSubject
,Protek = NEW.Protek
,CostType = NEW.CostType
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricesdataLogUpdate AFTER UPDATE
ON usersettings.pricesdata FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_data_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
pricesdataID = OLD.PriceCode
,FirmCode = IFNULL(NEW.FirmCode, OLD.FirmCode)
,RegionMask = NULLIF(NEW.RegionMask, OLD.RegionMask)
,AgencyEnabled = NULLIF(NEW.AgencyEnabled, OLD.AgencyEnabled)
,Enabled = NULLIF(NEW.Enabled, OLD.Enabled)
,ShowInWeb = NULLIF(NEW.ShowInWeb, OLD.ShowInWeb)
,AlowInt = NULLIF(NEW.AlowInt, OLD.AlowInt)
,PriceType = NULLIF(NEW.PriceType, OLD.PriceType)
,PriceName = NULLIF(NEW.PriceName, OLD.PriceName)
,MinReq = NULLIF(NEW.MinReq, OLD.MinReq)
,upcost = NULLIF(NEW.upcost, OLD.upcost)
,priceinfo = NULLIF(NEW.priceinfo, OLD.priceinfo)
,percentcorr = NULLIF(NEW.percentcorr, OLD.percentcorr)
,OrderEmailSubject = NULLIF(NEW.OrderEmailSubject, OLD.OrderEmailSubject)
,Protek = NULLIF(NEW.Protek, OLD.Protek)
,CostType = NULLIF(NEW.CostType, OLD.CostType)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricesdataLogDelete AFTER DELETE
ON usersettings.pricesdata FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_data_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
pricesdataID = OLD.PriceCode
,FirmCode = OLD.FirmCode
,RegionMask = OLD.RegionMask
,AgencyEnabled = OLD.AgencyEnabled
,Enabled = OLD.Enabled
,ShowInWeb = OLD.ShowInWeb
,AlowInt = OLD.AlowInt
,PriceType = OLD.PriceType
,PriceName = OLD.PriceName
,MinReq = OLD.MinReq
,upcost = OLD.upcost
,priceinfo = OLD.priceinfo
,percentcorr = OLD.percentcorr
,OrderEmailSubject = OLD.OrderEmailSubject
,Protek = OLD.Protek
,CostType = OLD.CostType
;END ;;

DELIMITER ;

-- восстанавливаем FormRules

DELIMITER ;;

CREATE TRIGGER farm.FormRulesLogInsert AFTER Insert ON farm.FormRules FOR EACH ROW
BEGIN
INSERT INTO `logs`.form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
FormRulesID = NEW.Id
,PriceFormatId = NEW.PriceFormatId
,MaxOld = NEW.MaxOld
,Delimiter = NEW.Delimiter
,ParentFormRules = NEW.ParentFormRules
,ParentSynonym = NEW.ParentSynonym
,FormByCode = NEW.FormByCode
,NameMask = NEW.NameMask
,ForbWords = NEW.ForbWords
,JunkPos = NEW.JunkPos
,AwaitPos = NEW.AwaitPos
,StartLine = NEW.StartLine
,ListName = NEW.ListName
,TxtCodeBegin = NEW.TxtCodeBegin
,TxtCodeEnd = NEW.TxtCodeEnd
,TxtCodeCrBegin = NEW.TxtCodeCrBegin
,TxtCodeCrEnd = NEW.TxtCodeCrEnd
,TxtNameBegin = NEW.TxtNameBegin
,TxtNameEnd = NEW.TxtNameEnd
,TxtFirmCrBegin = NEW.TxtFirmCrBegin
,TxtFirmCrEnd = NEW.TxtFirmCrEnd
,TxtCountryCrBegin = NEW.TxtCountryCrBegin
,TxtCountryCrEnd = NEW.TxtCountryCrEnd
,TxtBaseCostBegin = NEW.TxtBaseCostBegin
,TxtBaseCostEnd = NEW.TxtBaseCostEnd
,TxtMinBoundCostBegin = NEW.TxtMinBoundCostBegin
,TxtMinBoundCostEnd = NEW.TxtMinBoundCostEnd
,TxtCurrencyBegin = NEW.TxtCurrencyBegin
,TxtCurrencyEnd = NEW.TxtCurrencyEnd
,TxtUnitBegin = NEW.TxtUnitBegin
,TxtUnitEnd = NEW.TxtUnitEnd
,TxtVolumeBegin = NEW.TxtVolumeBegin
,TxtVolumeEnd = NEW.TxtVolumeEnd
,TxtQuantityBegin = NEW.TxtQuantityBegin
,TxtQuantityEnd = NEW.TxtQuantityEnd
,TxtNoteBegin = NEW.TxtNoteBegin
,TxtNoteEnd = NEW.TxtNoteEnd
,TxtPeriodBegin = NEW.TxtPeriodBegin
,TxtPeriodEnd = NEW.TxtPeriodEnd
,TxtDocBegin = NEW.TxtDocBegin
,TxtDocEnd = NEW.TxtDocEnd
,TxtJunkBegin = NEW.TxtJunkBegin
,TxtJunkEnd = NEW.TxtJunkEnd
,TxtAwaitBegin = NEW.TxtAwaitBegin
,TxtAwaitEnd = NEW.TxtAwaitEnd
,FCode = NEW.FCode
,FCodeCr = NEW.FCodeCr
,FName1 = NEW.FName1
,FName2 = NEW.FName2
,FName3 = NEW.FName3
,FFirmCr = NEW.FFirmCr
,FCountryCr = NEW.FCountryCr
,FBaseCost = NEW.FBaseCost
,FMinBoundCost = NEW.FMinBoundCost
,FCurrency = NEW.FCurrency
,FUnit = NEW.FUnit
,FVolume = NEW.FVolume
,FQuantity = NEW.FQuantity
,FNote = NEW.FNote
,FPeriod = NEW.FPeriod
,FDoc = NEW.FDoc
,FJunk = NEW.FJunk
,FAwait = NEW.FAwait
,Memo = NEW.Memo
,Currency = NEW.Currency
,TxtVitallyImportantBegin = NEW.TxtVitallyImportantBegin
,TxtVitallyImportantEnd = NEW.TxtVitallyImportantEnd
,FVitallyImportant = NEW.FVitallyImportant
,VitallyImportantMask = NEW.VitallyImportantMask
,TxtRequestRatioBegin = NEW.TxtRequestRatioBegin
,TxtRequestRatioEnd = NEW.TxtRequestRatioEnd
,FRequestRatio = NEW.FRequestRatio
,TxtRegistryCostBegin = NEW.TxtRegistryCostBegin
,TxtRegistryCostEnd = NEW.TxtRegistryCostEnd
,FRegistryCost = NEW.FRegistryCost
,FMaxBoundCost = NEW.FMaxBoundCost
,TxtMaxBoundCostBegin = NEW.TxtMaxBoundCostBegin
,TxtMaxBoundCostEnd = NEW.TxtMaxBoundCostEnd
,FOrderCost = NEW.FOrderCost
,TxtOrderCostBegin = NEW.TxtOrderCostBegin
,TxtOrderCostEnd = NEW.TxtOrderCostEnd
,FMinOrderCount = NEW.FMinOrderCount
,TxtMinOrderCountBegin = NEW.TxtMinOrderCountBegin
,TxtMinOrderCountEnd = NEW.TxtMinOrderCountEnd
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.FormRulesLogUpdate AFTER Update ON farm.FormRules FOR EACH ROW
BEGIN
INSERT INTO `logs`.form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
FormRulesID = OLD.Id
,PriceFormatId = IFNULL(NEW.PriceFormatId, OLD.PriceFormatId)
,MaxOld = NULLIF(NEW.MaxOld, OLD.MaxOld)
,Delimiter = NULLIF(NEW.Delimiter, OLD.Delimiter)
,ParentFormRules = IFNULL(NEW.ParentFormRules, OLD.ParentFormRules)
,ParentSynonym = IFNULL(NEW.ParentSynonym, OLD.ParentSynonym)
,FormByCode = NULLIF(NEW.FormByCode, OLD.FormByCode)
,NameMask = NULLIF(NEW.NameMask, OLD.NameMask)
,ForbWords = NULLIF(NEW.ForbWords, OLD.ForbWords)
,JunkPos = NULLIF(NEW.JunkPos, OLD.JunkPos)
,AwaitPos = NULLIF(NEW.AwaitPos, OLD.AwaitPos)
,StartLine = NULLIF(NEW.StartLine, OLD.StartLine)
,ListName = NULLIF(NEW.ListName, OLD.ListName)
,TxtCodeBegin = NULLIF(NEW.TxtCodeBegin, OLD.TxtCodeBegin)
,TxtCodeEnd = NULLIF(NEW.TxtCodeEnd, OLD.TxtCodeEnd)
,TxtCodeCrBegin = NULLIF(NEW.TxtCodeCrBegin, OLD.TxtCodeCrBegin)
,TxtCodeCrEnd = NULLIF(NEW.TxtCodeCrEnd, OLD.TxtCodeCrEnd)
,TxtNameBegin = NULLIF(NEW.TxtNameBegin, OLD.TxtNameBegin)
,TxtNameEnd = NULLIF(NEW.TxtNameEnd, OLD.TxtNameEnd)
,TxtFirmCrBegin = NULLIF(NEW.TxtFirmCrBegin, OLD.TxtFirmCrBegin)
,TxtFirmCrEnd = NULLIF(NEW.TxtFirmCrEnd, OLD.TxtFirmCrEnd)
,TxtCountryCrBegin = NULLIF(NEW.TxtCountryCrBegin, OLD.TxtCountryCrBegin)
,TxtCountryCrEnd = NULLIF(NEW.TxtCountryCrEnd, OLD.TxtCountryCrEnd)
,TxtBaseCostBegin = NULLIF(NEW.TxtBaseCostBegin, OLD.TxtBaseCostBegin)
,TxtBaseCostEnd = NULLIF(NEW.TxtBaseCostEnd, OLD.TxtBaseCostEnd)
,TxtMinBoundCostBegin = NULLIF(NEW.TxtMinBoundCostBegin, OLD.TxtMinBoundCostBegin)
,TxtMinBoundCostEnd = NULLIF(NEW.TxtMinBoundCostEnd, OLD.TxtMinBoundCostEnd)
,TxtCurrencyBegin = NULLIF(NEW.TxtCurrencyBegin, OLD.TxtCurrencyBegin)
,TxtCurrencyEnd = NULLIF(NEW.TxtCurrencyEnd, OLD.TxtCurrencyEnd)
,TxtUnitBegin = NULLIF(NEW.TxtUnitBegin, OLD.TxtUnitBegin)
,TxtUnitEnd = NULLIF(NEW.TxtUnitEnd, OLD.TxtUnitEnd)
,TxtVolumeBegin = NULLIF(NEW.TxtVolumeBegin, OLD.TxtVolumeBegin)
,TxtVolumeEnd = NULLIF(NEW.TxtVolumeEnd, OLD.TxtVolumeEnd)
,TxtQuantityBegin = NULLIF(NEW.TxtQuantityBegin, OLD.TxtQuantityBegin)
,TxtQuantityEnd = NULLIF(NEW.TxtQuantityEnd, OLD.TxtQuantityEnd)
,TxtNoteBegin = NULLIF(NEW.TxtNoteBegin, OLD.TxtNoteBegin)
,TxtNoteEnd = NULLIF(NEW.TxtNoteEnd, OLD.TxtNoteEnd)
,TxtPeriodBegin = NULLIF(NEW.TxtPeriodBegin, OLD.TxtPeriodBegin)
,TxtPeriodEnd = NULLIF(NEW.TxtPeriodEnd, OLD.TxtPeriodEnd)
,TxtDocBegin = NULLIF(NEW.TxtDocBegin, OLD.TxtDocBegin)
,TxtDocEnd = NULLIF(NEW.TxtDocEnd, OLD.TxtDocEnd)
,TxtJunkBegin = NULLIF(NEW.TxtJunkBegin, OLD.TxtJunkBegin)
,TxtJunkEnd = NULLIF(NEW.TxtJunkEnd, OLD.TxtJunkEnd)
,TxtAwaitBegin = NULLIF(NEW.TxtAwaitBegin, OLD.TxtAwaitBegin)
,TxtAwaitEnd = NULLIF(NEW.TxtAwaitEnd, OLD.TxtAwaitEnd)
,FCode = NULLIF(NEW.FCode, OLD.FCode)
,FCodeCr = NULLIF(NEW.FCodeCr, OLD.FCodeCr)
,FName1 = NULLIF(NEW.FName1, OLD.FName1)
,FName2 = NULLIF(NEW.FName2, OLD.FName2)
,FName3 = NULLIF(NEW.FName3, OLD.FName3)
,FFirmCr = NULLIF(NEW.FFirmCr, OLD.FFirmCr)
,FCountryCr = NULLIF(NEW.FCountryCr, OLD.FCountryCr)
,FBaseCost = NULLIF(NEW.FBaseCost, OLD.FBaseCost)
,FMinBoundCost = NULLIF(NEW.FMinBoundCost, OLD.FMinBoundCost)
,FCurrency = NULLIF(NEW.FCurrency, OLD.FCurrency)
,FUnit = NULLIF(NEW.FUnit, OLD.FUnit)
,FVolume = NULLIF(NEW.FVolume, OLD.FVolume)
,FQuantity = NULLIF(NEW.FQuantity, OLD.FQuantity)
,FNote = NULLIF(NEW.FNote, OLD.FNote)
,FPeriod = NULLIF(NEW.FPeriod, OLD.FPeriod)
,FDoc = NULLIF(NEW.FDoc, OLD.FDoc)
,FJunk = NULLIF(NEW.FJunk, OLD.FJunk)
,FAwait = NULLIF(NEW.FAwait, OLD.FAwait)
,Memo = NULLIF(NEW.Memo, OLD.Memo)
,Currency = NULLIF(NEW.Currency, OLD.Currency)
,TxtVitallyImportantBegin = NULLIF(NEW.TxtVitallyImportantBegin, OLD.TxtVitallyImportantBegin)
,TxtVitallyImportantEnd = NULLIF(NEW.TxtVitallyImportantEnd, OLD.TxtVitallyImportantEnd)
,FVitallyImportant = NULLIF(NEW.FVitallyImportant, OLD.FVitallyImportant)
,VitallyImportantMask = NULLIF(NEW.VitallyImportantMask, OLD.VitallyImportantMask)
,TxtRequestRatioBegin = NULLIF(NEW.TxtRequestRatioBegin, OLD.TxtRequestRatioBegin)
,TxtRequestRatioEnd = NULLIF(NEW.TxtRequestRatioEnd, OLD.TxtRequestRatioEnd)
,FRequestRatio = NULLIF(NEW.FRequestRatio, OLD.FRequestRatio)
,TxtRegistryCostBegin = NULLIF(NEW.TxtRegistryCostBegin, OLD.TxtRegistryCostBegin)
,TxtRegistryCostEnd = NULLIF(NEW.TxtRegistryCostEnd, OLD.TxtRegistryCostEnd)
,FRegistryCost = NULLIF(NEW.FRegistryCost, OLD.FRegistryCost)
,FMaxBoundCost = NULLIF(NEW.FMaxBoundCost, OLD.FMaxBoundCost)
,TxtMaxBoundCostBegin = NULLIF(NEW.TxtMaxBoundCostBegin, OLD.TxtMaxBoundCostBegin)
,TxtMaxBoundCostEnd = NULLIF(NEW.TxtMaxBoundCostEnd, OLD.TxtMaxBoundCostEnd)
,FOrderCost = NULLIF(NEW.FOrderCost, OLD.FOrderCost)
,TxtOrderCostBegin = NULLIF(NEW.TxtOrderCostBegin, OLD.TxtOrderCostBegin)
,TxtOrderCostEnd = NULLIF(NEW.TxtOrderCostEnd, OLD.TxtOrderCostEnd)
,FMinOrderCount = NULLIF(NEW.FMinOrderCount, OLD.FMinOrderCount)
,TxtMinOrderCountBegin = NULLIF(NEW.TxtMinOrderCountBegin, OLD.TxtMinOrderCountBegin)
,TxtMinOrderCountEnd = NULLIF(NEW.TxtMinOrderCountEnd, OLD.TxtMinOrderCountEnd)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.FormRulesLogDelete AFTER Delete ON farm.FormRules FOR EACH ROW
BEGIN
INSERT INTO `logs`.form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
FormRulesID = OLD.Id
,PriceFormatId = OLD.PriceFormatId
,MaxOld = OLD.MaxOld
,Delimiter = OLD.Delimiter
,ParentFormRules = OLD.ParentFormRules
,ParentSynonym = OLD.ParentSynonym
,FormByCode = OLD.FormByCode
,NameMask = OLD.NameMask
,ForbWords = OLD.ForbWords
,JunkPos = OLD.JunkPos
,AwaitPos = OLD.AwaitPos
,StartLine = OLD.StartLine
,ListName = OLD.ListName
,TxtCodeBegin = OLD.TxtCodeBegin
,TxtCodeEnd = OLD.TxtCodeEnd
,TxtCodeCrBegin = OLD.TxtCodeCrBegin
,TxtCodeCrEnd = OLD.TxtCodeCrEnd
,TxtNameBegin = OLD.TxtNameBegin
,TxtNameEnd = OLD.TxtNameEnd
,TxtFirmCrBegin = OLD.TxtFirmCrBegin
,TxtFirmCrEnd = OLD.TxtFirmCrEnd
,TxtCountryCrBegin = OLD.TxtCountryCrBegin
,TxtCountryCrEnd = OLD.TxtCountryCrEnd
,TxtBaseCostBegin = OLD.TxtBaseCostBegin
,TxtBaseCostEnd = OLD.TxtBaseCostEnd
,TxtMinBoundCostBegin = OLD.TxtMinBoundCostBegin
,TxtMinBoundCostEnd = OLD.TxtMinBoundCostEnd
,TxtCurrencyBegin = OLD.TxtCurrencyBegin
,TxtCurrencyEnd = OLD.TxtCurrencyEnd
,TxtUnitBegin = OLD.TxtUnitBegin
,TxtUnitEnd = OLD.TxtUnitEnd
,TxtVolumeBegin = OLD.TxtVolumeBegin
,TxtVolumeEnd = OLD.TxtVolumeEnd
,TxtQuantityBegin = OLD.TxtQuantityBegin
,TxtQuantityEnd = OLD.TxtQuantityEnd
,TxtNoteBegin = OLD.TxtNoteBegin
,TxtNoteEnd = OLD.TxtNoteEnd
,TxtPeriodBegin = OLD.TxtPeriodBegin
,TxtPeriodEnd = OLD.TxtPeriodEnd
,TxtDocBegin = OLD.TxtDocBegin
,TxtDocEnd = OLD.TxtDocEnd
,TxtJunkBegin = OLD.TxtJunkBegin
,TxtJunkEnd = OLD.TxtJunkEnd
,TxtAwaitBegin = OLD.TxtAwaitBegin
,TxtAwaitEnd = OLD.TxtAwaitEnd
,FCode = OLD.FCode
,FCodeCr = OLD.FCodeCr
,FName1 = OLD.FName1
,FName2 = OLD.FName2
,FName3 = OLD.FName3
,FFirmCr = OLD.FFirmCr
,FCountryCr = OLD.FCountryCr
,FBaseCost = OLD.FBaseCost
,FMinBoundCost = OLD.FMinBoundCost
,FCurrency = OLD.FCurrency
,FUnit = OLD.FUnit
,FVolume = OLD.FVolume
,FQuantity = OLD.FQuantity
,FNote = OLD.FNote
,FPeriod = OLD.FPeriod
,FDoc = OLD.FDoc
,FJunk = OLD.FJunk
,FAwait = OLD.FAwait
,Memo = OLD.Memo
,Currency = OLD.Currency
,TxtVitallyImportantBegin = OLD.TxtVitallyImportantBegin
,TxtVitallyImportantEnd = OLD.TxtVitallyImportantEnd
,FVitallyImportant = OLD.FVitallyImportant
,VitallyImportantMask = OLD.VitallyImportantMask
,TxtRequestRatioBegin = OLD.TxtRequestRatioBegin
,TxtRequestRatioEnd = OLD.TxtRequestRatioEnd
,FRequestRatio = OLD.FRequestRatio
,TxtRegistryCostBegin = OLD.TxtRegistryCostBegin
,TxtRegistryCostEnd = OLD.TxtRegistryCostEnd
,FRegistryCost = OLD.FRegistryCost
,FMaxBoundCost = OLD.FMaxBoundCost
,TxtMaxBoundCostBegin = OLD.TxtMaxBoundCostBegin
,TxtMaxBoundCostEnd = OLD.TxtMaxBoundCostEnd
,FOrderCost = OLD.FOrderCost
,TxtOrderCostBegin = OLD.TxtOrderCostBegin
,TxtOrderCostEnd = OLD.TxtOrderCostEnd
,FMinOrderCount = OLD.FMinOrderCount
,TxtMinOrderCountBegin = OLD.TxtMinOrderCountBegin
,TxtMinOrderCountEnd = OLD.TxtMinOrderCountEnd
,FormRulesID = OLD.Id
,PriceFormatId = OLD.PriceFormatId
;END ;;

DELIMITER ;


-- восстанавливаем логирование в Sources

alter table logs.sources_logs
  drop column `SourceType`,
  drop column `LastDateTime`,
  drop column `PriceDateTime`,
  drop column `EMailPassword`,
  add column `SourceTypeId` int(11) unsigned default NULL after `sourcesId`,
  add column `FTPPassiveMode` tinyint(1) default NULL after `FTPPassword`;


DELIMITER ;;

CREATE TRIGGER farm.sourcesLogInsert AFTER Insert ON farm.sources FOR EACH ROW
BEGIN
INSERT INTO `logs`.sources_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
sourcesID = NEW.Id
,SourceTypeId = NEW.SourceTypeId
,PricePath = NEW.PricePath
,EMailTo = NEW.EMailTo
,EMailFrom = NEW.EMailFrom
,FTPDir = NEW.FTPDir
,FTPLogin = NEW.FTPLogin
,FTPPassword = NEW.FTPPassword
,FTPPassiveMode = NEW.FTPPassiveMode
,PriceMask = NEW.PriceMask
,ExtrMask = NEW.ExtrMask
,HTTPLogin = NEW.HTTPLogin
,HTTPPassword = NEW.HTTPPassword
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.sourcesLogUpdate AFTER Update ON farm.sources FOR EACH ROW
BEGIN
INSERT INTO `logs`.sources_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
sourcesID = OLD.Id
,SourceTypeId = IFNULL(NEW.SourceTypeId, OLD.SourceTypeId)
,PricePath = NULLIF(NEW.PricePath, OLD.PricePath)
,EMailTo = NULLIF(NEW.EMailTo, OLD.EMailTo)
,EMailFrom = NULLIF(NEW.EMailFrom, OLD.EMailFrom)
,FTPDir = NULLIF(NEW.FTPDir, OLD.FTPDir)
,FTPLogin = NULLIF(NEW.FTPLogin, OLD.FTPLogin)
,FTPPassword = NULLIF(NEW.FTPPassword, OLD.FTPPassword)
,FTPPassiveMode = NULLIF(NEW.FTPPassiveMode, OLD.FTPPassiveMode)
,PriceMask = NULLIF(NEW.PriceMask, OLD.PriceMask)
,ExtrMask = NULLIF(NEW.ExtrMask, OLD.ExtrMask)
,HTTPLogin = NULLIF(NEW.HTTPLogin, OLD.HTTPLogin)
,HTTPPassword = NULLIF(NEW.HTTPPassword, OLD.HTTPPassword)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.sourcesLogDelete AFTER Delete ON farm.sources FOR EACH ROW
BEGIN
INSERT INTO `logs`.sources_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
sourcesID = OLD.Id
,SourceTypeId = OLD.SourceTypeId
,PricePath = OLD.PricePath
,EMailTo = OLD.EMailTo
,EMailFrom = OLD.EMailFrom
,FTPDir = OLD.FTPDir
,FTPLogin = OLD.FTPLogin
,FTPPassword = OLD.FTPPassword
,FTPPassiveMode = OLD.FTPPassiveMode
,PriceMask = OLD.PriceMask
,ExtrMask = OLD.ExtrMask
,HTTPLogin = OLD.HTTPLogin
,HTTPPassword = OLD.HTTPPassword
;END ;;

DELIMITER ;


-- восстанавливаем триггеры для обновления PriceItems

drop trigger if exists Catalogs.AssortmentAfterInsert;

drop trigger if exists farm.AssortmentAfterInsert;


DELIMITER ;;

CREATE TRIGGER Catalogs.AssortmentAfterInsert AFTER INSERT ON Catalogs.Assortment FOR EACH ROW
BEGIN
  update
    usersettings.PriceItems,
    usersettings.PricesCosts
  set
    PriceItems.PriceDate = now(),
    PriceItems.LastFormalization = now()
  where
        PricesCosts.PriceCode = 2647
    and PricesCosts.BaseCost = 1
    and PriceItems.Id = PricesCosts.PriceItemId;
end ;;

DELIMITER ;


-- изменяем процедуры в базе reports

DELIMITER $$

DROP PROCEDURE IF EXISTS reports.GetPriceCode $$

CREATE PROCEDURE reports.GetPriceCode(IN inFirmCode BIGINT, IN inFilter VARCHAR(255), IN inID BIGINT)
BEGIN
  DECLARE filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  ENGINE=memory
  SELECT
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', cd.ShortName, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) AS PriceName
  FROM
    usersettings.pricesdata pd
    inner join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = cd.RegionCode
  where
      cd.FirmType = 0
  and cd.FirmStatus = 1
  and pd.AgencyEnabled = 1
  and pd.Enabled = 1;
  if (inID is not null) then
    select
      tmp.PriceCode as ID,
      tmp.PriceName as DisplayValue
    from
      tempGetPriceCode tmp
    where
      tmp.PriceCode = inID
    order by tmp.PriceName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      where
        tmp.PriceName like filterStr
      order by tmp.PriceName;
    else
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      order by tmp.PriceName;
    end if;
  end if;
  drop table if exists tempGetPriceCode;
END $$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS reports.GetAllPriceCode $$

CREATE PROCEDURE reports.GetAllPriceCode(IN inFirmCode BIGINT, IN inFilter VARCHAR(255), IN inID BIGINT)
BEGIN
  DECLARE filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  ENGINE=memory
  SELECT
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', cd.ShortName, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) AS PriceName
  FROM
    usersettings.pricesdata pd
    inner join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = cd.RegionCode
  where
      cd.FirmType = 0;
  if (inID is not null) then
    select
      tmp.PriceCode as ID,
      tmp.PriceName as DisplayValue
    from
      tempGetPriceCode tmp
    where
      tmp.PriceCode = inID
    order by tmp.PriceName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      where
        tmp.PriceName like filterStr
      order by tmp.PriceName;
    else
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      order by tmp.PriceName;
    end if;
  end if;
  drop table if exists tempGetPriceCode;
END $$

DELIMITER ;


-- изменяем процедуру usersettings.GetStatLog

DELIMITER $$

DROP PROCEDURE IF EXISTS usersettings.GetStatLog $$

PROCEDURE usersettings.GetStatLog(IN inLogStart DATETIME, IN inLogEnd DATETIME)
BEGIN
  
  drop temporary table if exists  tempdownlogs;
  create temporary table tempdownlogs (
    rowid int unsigned,
    priceitemid int unsigned,
    resultcode mediumint unsigned,
    fixedtime datetime,
    key (rowid),
    key (priceitemid),
    key (resultcode)) engine=MEMORY;
  insert into tempdownlogs
    SELECT
      rowid, priceitemid, resultcode, null
    FROM
      logs.downlogs d
    where (inLogStart < logtime) and (logtime < inLogEnd);
  
  select
    min(Rowid)
  into @minrowid
  from
    tempdownlogs
  where
        resultcode in (3, 5);
  update tempdownlogs, logs.downlogs dl
  set
    fixedtime = dl.logtime
  where
        tempdownlogs.priceitemid=dl.priceitemid
    and tempdownlogs.rowid >= @minrowid
    and @minrowid < dl.rowid
    and tempdownlogs.rowid < dl.rowid 
    and dl.resultcode = 2
    and tempdownlogs.resultcode in (3, 5);
  
  drop temporary table if exists  tempformlogs;
  create temporary table tempformlogs (
    rowid int unsigned primary key,
    logtime datetime,
    priceitemid int unsigned,
    resultid mediumint unsigned,
    fixedtime datetime,
    key priceitemid(rowid, priceitemid),
    key resultid(rowid, resultid)) engine=MEMORY;
  insert into tempformlogs
    SELECT
      rowid, logtime, priceitemid, resultid, null
    FROM
      logs.formlogs d
    where (inLogStart < logtime) and (logtime < inLogEnd);
  update tempformlogs, logs.formlogs dl
  set
    fixedtime = dl.logtime
  where
        tempformlogs.priceitemid=dl.priceitemid
    and tempformlogs.rowid < dl.rowid
    and dl.resultid in (2, 3)
    and tempformlogs.resultid = 5;
SELECT
  1 As LAppCode,
  LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  null As LFirmName,
  null As LRegion,
  null As LFirmSegment,
  null as LPriceName,
  null As LPriceCode,
  null as LForm,
  null as LUnform,
  null as LZero,
  null as LForb,
  
  if(ResultCode = 2, null, Addition) As LAddition,
  ResultCode As LResultID,
  1 as LStatus
FROM
  logs.downlogs as logs
WHERE
    logs.priceitemid is null
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
union
SELECT
  1 As LAppCode,
  logs.LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  cd.ShortName As LFirmName,
  r.Region As LRegion,
  cd.FirmSegment As LFirmSegment,
  if(pd.CostType = 1, convert(concat(convert("[Колонка] " using cp1251), pc.CostName) using cp1251), pd.PriceName) as LPriceName,
  pd.PriceCode As LPriceCode,
  null as LForm,
  null as LUnform,
  null as LZero,
  null as LForb,
  
  if(logs.ResultCode = 2, null, logs.Addition) As LAddition,
  logs.ResultCode As LResultID,
  if((logs.ResultCode = 2), 0, if(tempdownlogs.fixedtime is null, 1, 2)) as LStatus
FROM
  logs.downlogs as logs,
  usersettings.PriceItems pim,
  usersettings.clientsdata cd,
  usersettings.pricesdata pd,
  usersettings.pricescosts pc,
  farm.regions r,
  tempdownlogs
WHERE
    pim.Id = logs.PriceItemId
and pc.PriceItemId = pim.Id
and pc.PriceCode = pd.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.firmcode=pd.firmcode
and r.regioncode=cd.regioncode
and tempdownlogs.RowId = logs.RowID
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
union
SELECT
  0 As LAppCode,
  LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  null As LFirmName,
  null As LRegion,
  null As LFirmSegment,
  null as LPriceName,
  null As LPriceCode,
  if(Form is null, 0, Form) As LForm,
  if(Unform is null, 0, Unform) As LUnform,
  if(Zero is null, 0, Zero) As LZero,
  if(Forb is null, 0, Forb) As LForb,
  Addition As LAddition,
  ResultID As LResultID,
  1 as LStatus
FROM
  logs.formlogs as logs
WHERE
    logs.PriceItemId is null
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
union
SELECT
  0 As LAppCode,
  logs.LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  cd.ShortName As LFirmName,
  r.Region As LRegion,
  cd.FirmSegment As LFirmSegment,
  if(pd.CostType = 1, convert(concat(convert("[Колонка] " using cp1251), pc.CostName) using cp1251), pd.PriceName) as LPriceName,
  pd.PriceCode As LPriceCode,
  if(logs.Form is null, 0, logs.Form) As LForm,
  if(logs.Unform is null, 0, logs.Unform) As LUnform,
  if(logs.Zero is null, 0, logs.Zero) As LZero,
  if(logs.Forb is null, 0, logs.Forb) As LForb,
  logs.Addition As LAddition,
  logs.ResultID As LResultID,
  if((logs.ResultID = 2) or (logs.ResultID = 3), 0, if(tempformlogs.fixedtime is null, 1, 2)) as LStatus
FROM
  logs.formlogs as logs,
  usersettings.PriceItems pim,
  usersettings.clientsdata cd,
  usersettings.pricesdata pd,
  usersettings.pricescosts pc,
  farm.regions r,
  tempformlogs
WHERE
    pim.Id = logs.PriceItemId
and pc.PriceItemId = pim.Id
and pc.PriceCode = pd.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.firmcode=pd.firmcode
and r.regioncode=cd.regioncode
and tempformlogs.RowId = logs.RowId
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
order by LLogTime;
END $$

DELIMITER ;

