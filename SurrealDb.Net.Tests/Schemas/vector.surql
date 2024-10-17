DEFINE TABLE vector SCHEMAFULL;

DEFINE FIELD name ON vector TYPE string;
DEFINE FIELD value ON vector TYPE option<array<number>>;

CREATE vector:none SET name = "none", value = NONE;
CREATE vector:empty SET name = "empty", value = [];
CREATE vector:vector2 SET name = "vector2", value = [2.5, 0.5];
CREATE vector:vector3 SET name = "vector3", value = [4, 9, 16];
CREATE vector:vector4 SET name = "vector4", value = [2, 3, 4, 5];