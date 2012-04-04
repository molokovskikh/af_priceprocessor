alter table Farm.CoreCosts
add column RequestRatio smallint(5) unsigned,
add column MinOrderSum decimal(8, 2) unsigned,
add column MinOrderCount int unsigned;
