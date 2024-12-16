DEFINE TABLE uuid SCHEMAFULL;

DEFINE FIELD name ON uuid TYPE string;
DEFINE FIELD value ON uuid TYPE option<uuid>;

CREATE uuid SET name = 'none', value = NONE;
CREATE uuid SET name = 'precise', value = <uuid> "a8f30d8b-db67-47ec-8b38-ef703e05ad1b";
CREATE uuid SET name = 'v4', value = rand::uuid::v4();
