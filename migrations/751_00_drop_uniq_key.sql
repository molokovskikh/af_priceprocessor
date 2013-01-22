alter table Farm.BuyingMatrix
drop foreign key FK_BuyingMatrix_PriceId;

alter table Farm.BuyingMatrix
drop index FK_BuyingMatrix_Comb;

alter table Farm.BuyingMatrix
add CONSTRAINT `FK_BuyingMatrix_PriceId` FOREIGN KEY (`PriceId`) REFERENCES `usersettings`.`pricesdata` (`PriceCode`) ON DELETE CASCADE;
