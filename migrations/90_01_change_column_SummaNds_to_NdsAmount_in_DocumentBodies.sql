ALTER TABLE documents.DocumentBodies
add column NdsAmount DECIMAL(12, 6) UNSIGNED DEFAULT NULL COMMENT 'Сумма НДС';