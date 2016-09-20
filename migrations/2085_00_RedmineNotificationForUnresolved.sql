use customers;

ALTER TABLE `clients`
	ADD COLUMN `RedmineNotificationForUnresolved` TINYINT(1) UNSIGNED NOT NULL DEFAULT '0' AFTER `FtpIntegration`;