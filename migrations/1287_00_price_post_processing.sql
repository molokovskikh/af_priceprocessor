alter table Usersettings.PricesData
add column PostProcessing VARCHAR(255),
change column IsUpdate IsUpdate tinyint(1) unsigned NOT NULL DEFAULT '1'
;
update Usersettings.PricesData
set IsUpdate = 1;
