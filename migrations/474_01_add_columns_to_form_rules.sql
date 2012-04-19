alter table Farm.FormRules
add column FEAN13 varchar(20),
add column TxtEAN13Begin int,
add column TxtEAN13End int,
add column FSeries varchar(20),
add column TxtSeriesBegin int,
add column TxtSeriesEnd int,
add column FCodeOKP varchar(20),
add column TxtCodeOKPBegin int,
add column TxtCodeOKPEnd int;
