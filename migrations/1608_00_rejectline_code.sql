use Documents;
ALTER TABLE `rejectlines`
	ADD COLUMN `Code` VARCHAR(255) NULL DEFAULT '0' AFTER `Id`;
