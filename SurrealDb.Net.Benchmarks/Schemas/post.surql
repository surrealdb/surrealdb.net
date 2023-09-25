DEFINE TABLE post SCHEMAFULL;

DEFINE FIELD title ON post TYPE string;
DEFINE FIELD content ON post TYPE string;
DEFINE FIELD created_at ON post TYPE datetime DEFAULT time::now();
DEFINE FIELD status ON post TYPE string DEFAULT 'DRAFT' ASSERT $value INSIDE ['DRAFT', 'PUBLISHED'];