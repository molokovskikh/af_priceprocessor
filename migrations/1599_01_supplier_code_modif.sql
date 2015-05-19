DROP EVENT Catalogs.SupplierCodeModif;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` EVENT Catalogs.`SupplierCodeModif` ON SCHEDULE EVERY 1 DAY STARTS '2013-01-17 14:40:00' ON COMPLETION PRESERVE ENABLE DO BEGIN
  DROP TEMPORARY TABLE IF EXISTS SupCodeDel;
  CREATE TEMPORARY TABLE SupCodeDel
  SELECT s.`Id`
  FROM
    suppliercodes s, usersettings.pricesdata p, farm.Core0 C
  WHERE
    s.`SupplierId` = p.`FirmCode`
    AND p.`PriceCode` = C.`PriceCode`
    AND C.`Code` = s.`Code`
    AND s.`CodeCr` = C.`CodeCr`
    AND (C.`SynonymCode` <> s.`SynonymId`
    OR s.`SynonymCrId` <> C.`SynonymFirmCrCode`);
  DELETE
  FROM
    s
  USING
    SupCodeDel SPD, suppliercodes s
  WHERE
    SPD.id = s.id;
  DROP TEMPORARY TABLE SupCodeDel;

  INSERT INTO catalogs.SupplierCodes (`supplierId`, `ProductId`, `ProducerId`, `SynonymId`, `SynonymCrId`, `Code`, `CodeCr`)
  SELECT P.`FirmCode`
       , C.`ProductId`
       , C.`CodeFirmCr`
       , C.`SynonymCode`
       , C.`SynonymFirmCrCode`
       , C.`Code`
       , C.`CodeCr`
  FROM
    farm.Core0 C
	JOIN usersettings.PricesData P ON P.`PriceCode` = C.`PriceCode`
	JOIN Customers.Suppliers sp ON sp.Id = p.FirmCode
  LEFT JOIN catalogs.suppliercodes s
  ON s.`ProductId` = C.`ProductId` AND C.`CodeFirmCr` = s.`ProducerId` AND C.`Code` = s.`Code` AND s.`CodeCr` = C.`CodeCr` AND s.`SupplierId` = P.`FirmCode`
  WHERE
    s.`Id` IS NULL
    AND C.`CodeFirmCr` IS NOT NULL
    AND P.`FirmCode` IS NOT NULL
    AND C.`SynonymFirmCrCode` IS NOT NULL
	and sp.UseSupplierCodes = 1
  GROUP BY
    C.`Code`
  , C.`CodeCr`
  , C.`ProductId`
  , C.`CodeFirmCr`;
END;
