DEFINE TABLE recordId SCHEMAFULL;

DEFINE FIELD name ON recordId TYPE string;

CREATE recordId SET name = 'auto-generated';
CREATE recordId:surrealdb SET name = 'string';
CREATE recordId:17493 SET name = 'number';
CREATE recordId:`8424486b-85b3-4448-ac8d-5d51083391c7` SET name = 'complex';
CREATE recordId:⟨f47ac10b-58cc-4372-a567-0e02b2c3d479⟩ SET name = 'complex-2';
CREATE recordId:{ location: 'London', year: 2023 } SET name = 'object';
CREATE recordId:['London', 2023] SET name = 'array';
CREATE recordId:rand() SET name = 'rand';
CREATE recordId:ulid() SET name = 'ulid';
CREATE recordId:uuid() SET name = 'uuid';