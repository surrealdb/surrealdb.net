DEFINE TABLE post SCHEMAFULL 
    PERMISSIONS
        FOR select, create, update, delete WHERE $auth.id != NONE;

DEFINE FIELD title ON post TYPE string;
DEFINE FIELD content ON post TYPE string;
DEFINE FIELD created_at ON post TYPE datetime DEFAULT time::now();
DEFINE FIELD status ON post TYPE string DEFAULT 'DRAFT' ASSERT $value INSIDE ['DRAFT', 'PUBLISHED'];

CREATE post:first SET title = "First article", content = "This is my first article";
CREATE post SET title = "Second article", content = "Another article";