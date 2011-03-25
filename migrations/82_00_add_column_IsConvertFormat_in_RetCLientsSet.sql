ALTER TABLE usersettings.RetClientsSet 
ADD COLUMN IsConvertFormat TINYINT(1) UNSIGNED NOT NULL DEFAULT 0
COMMENT 'Преобразование в формат dbf';