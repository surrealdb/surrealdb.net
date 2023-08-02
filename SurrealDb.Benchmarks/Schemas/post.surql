DEFINE TABLE post SCHEMAFULL;

DEFINE FIELD title ON post TYPE string;
DEFINE FIELD content ON post TYPE string;
DEFINE FIELD created_at ON post TYPE datetime VALUE $before OR time::now();
DEFINE FIELD status ON post TYPE string VALUE $value OR $before OR 'DRAFT' ASSERT $value == NONE OR $value INSIDE ['DRAFT', 'PUBLISHED'];