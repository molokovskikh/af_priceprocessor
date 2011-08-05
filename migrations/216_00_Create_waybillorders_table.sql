CREATE TABLE  `documents`.`waybillorders` (
  `DocumentLineId` int(10) unsigned NOT NULL COMMENT 'id позиции в DocumentBodies',
  `OrderLineId` int(10) unsigned NOT NULL COMMENT 'id позиции в OrdersList'

) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
