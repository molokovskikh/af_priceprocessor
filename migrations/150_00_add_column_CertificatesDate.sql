ALTER TABLE `documents`.`documentbodies`
ADD COLUMN `CertificatesDate` VARCHAR(20) COMMENT 'Дата выдачи сертификата'
AFTER `Certificates`;