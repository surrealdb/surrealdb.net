DEFINE TABLE address SCHEMALESS;

DEFINE FIELD number ON address TYPE string;
DEFINE FIELD street ON address TYPE string;
DEFINE FIELD city ON address TYPE string;
DEFINE FIELD state ON address TYPE string;
DEFINE FIELD zip_code ON address TYPE string;
DEFINE FIELD country ON address TYPE string;

---------------------------------------------
DEFINE TABLE customer SCHEMALESS;

DEFINE FIELD name ON customer TYPE string;
DEFINE FIELD email ON customer TYPE string;
DEFINE FIELD address ON customer TYPE record(address);

---------------------------------------------
DEFINE TABLE product SCHEMALESS;

DEFINE FIELD name ON product TYPE string;
DEFINE FIELD description ON product TYPE string;
DEFINE FIELD price ON product TYPE number;
DEFINE FIELD category ON product TYPE string;
DEFINE FIELD images ON product TYPE array;

---------------------------------------------
# in: customer
# out: product
DEFINE TABLE purchased SCHEMALESS;

DEFINE FIELD quantity ON purchased TYPE number;
DEFINE FIELD shipping_address ON purchased TYPE record(address);
DEFINE FIELD created_at ON purchased TYPE datetime VALUE $before OR time::now();
DEFINE FIELD shipped_at ON purchased TYPE datetime;
DEFINE FIELD total ON purchased TYPE number;
DEFINE FIELD status ON purchased TYPE string VALUE $value OR $before OR 'Pending' ASSERT $value == NONE OR $value INSIDE ['Pending', 'Delivered'];

---------------------------------------------
DEFINE TABLE purchase SCHEMALESS;

DEFINE FIELD customer ON purchase TYPE record(customer);
DEFINE FIELD product ON purchase TYPE record(product);
DEFINE FIELD quantity ON purchase TYPE number;
DEFINE FIELD total ON purchase TYPE number;

DEFINE EVENT purchase ON TABLE purchase WHEN $before == NONE THEN {
    LET $from = (SELECT * FROM customer WHERE id == $after.customer);
    LET $to = (SELECT * FROM product WHERE id == $after.product);

    RELATE $from->purchased->$to CONTENT {
        quantity: $after.quantity,
        total: $after.total,
        status: 'Pending',
    };
};