alter table Farm.BuyingMatrix
add column IgnoreInBlackList tinyint(1) not null default '0';
