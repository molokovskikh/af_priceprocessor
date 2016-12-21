USE farm;
ALTER TABLE `unrecexp` ADD COLUMN ManualDel TINYINT(1) NOT NULL DEFAULT 1;
UPDATE `unrecexp` SET ManualDel = 0;
