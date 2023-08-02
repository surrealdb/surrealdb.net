DEFINE TABLE string SCHEMAFULL;

DEFINE FIELD name ON string TYPE string;
DEFINE FIELD value ON string TYPE string;

CREATE string SET name = 'none', value = NONE;
CREATE string SET name = 'single-quote', value = 'Lorem ipsum dolor sit amet';
CREATE string SET name = 'double-quote', value = "Lorem ipsum dolor sit amet";
CREATE string SET name = 'unicode', value = "I ❤️ SurrealDB";
CREATE string SET name = 'multiline', value = "This
is
over
multiple
lines";