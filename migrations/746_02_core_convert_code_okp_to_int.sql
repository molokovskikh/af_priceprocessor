update Farm.Core0
set CodeOKP = replace(CodeOKP, ' ', '')
where CodeOKP <> '';

alter table Farm.Core0
change column CodeOKP CodeOKP int unsigned;
