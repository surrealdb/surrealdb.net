DEFINE TABLE number SCHEMAFULL;

DEFINE FIELD name ON number TYPE string;
DEFINE FIELD value ON number TYPE option<number>;

CREATE number SET name = 'none', value = NONE;
CREATE number SET name = 'min', value = -9223372036854775808;
CREATE number SET name = 'max', value = 9223372036854775807;
CREATE number SET name = 'zero', value = 0;