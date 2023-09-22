DEFINE TABLE thing SCHEMAFULL;

DEFINE FIELD name ON thing TYPE string;

CREATE thing SET name = 'auto-generated';
CREATE thing:surrealdb SET name = 'string';
CREATE thing:17493 SET name = 'number';
CREATE thing:`8424486b-85b3-4448-ac8d-5d51083391c7` SET name = 'complex';
CREATE thing:⟨f47ac10b-58cc-4372-a567-0e02b2c3d479⟩ SET name = 'complex-2';
CREATE thing:{ location: 'London', year: 2023 } SET name = 'object';
CREATE thing:['London', 2023] SET name = 'array';
CREATE thing:rand() SET name = 'rand';
CREATE thing:ulid() SET name = 'ulid';
CREATE thing:uuid() SET name = 'uuid';