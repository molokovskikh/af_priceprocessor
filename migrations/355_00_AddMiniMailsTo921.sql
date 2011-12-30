insert into contacts.contact_groups
(Name, Type, Public, ContactGroupOwnerId, Specialized)
select
'Список E-mail, с которых разрешена отправка писем клиентам АналитФармация' as Name,
10 as Type,
1 as Public,
Suppliers.ContactGroupOwnerId,
0 as Specialized
from
future.Suppliers
left join contacts.contact_groups on contact_groups.ContactGroupOwnerId = Suppliers.ContactGroupOwnerId and contact_groups.Type = 10
where
Suppliers.Payer = 921
and contact_groups.Id is null;