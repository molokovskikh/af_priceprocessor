USE documents;
ALTER TABLE `rejectheaders`
	ADD COLUMN `Parser` VARCHAR(255) NULL AFTER `DownloadId`;