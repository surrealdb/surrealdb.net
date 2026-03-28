DEFINE TABLE user SCHEMAFULL
    PERMISSIONS
        FOR select, create, update, delete WHERE $auth.id != NONE;

DEFINE FIELD name ON user TYPE string;
DEFINE FIELD email ON user TYPE string;
DEFINE FIELD created_at ON user TYPE datetime DEFAULT time::now();

CREATE user:alice SET name = "Alice Example", email = "alice@example.com";
CREATE user:bob SET name = "Bob Buyer", email = "bob@example.com";
CREATE user:charlie SET name = "Charlie Client", email = "charlie@example.com";
CREATE user:diana SET name = "Diana Designer", email = "diana@example.com";
CREATE user:eve SET name = "Eve Engineer", email = "eve@example.com";
CREATE user:frank SET name = "Frank Founder", email = "frank@example.com";


DEFINE TABLE product SCHEMAFULL
    PERMISSIONS
        FOR select, create, update, delete WHERE $auth.id != NONE;

DEFINE FIELD name ON product TYPE string;
DEFINE FIELD description ON product TYPE string;
DEFINE FIELD price ON product TYPE float;
DEFINE FIELD created_at ON product TYPE datetime DEFAULT time::now();

CREATE product:book SET name = "Book", description = "A great book", price = 19.99;
CREATE product:laptop SET name = "Laptop", description = "A fast laptop", price = 999.99;
CREATE product:mouse SET name = "Mouse", description = "Wireless mouse", price = 29.99;
CREATE product:keyboard SET name = "Keyboard", description = "Mechanical keyboard", price = 89.99;
CREATE product:monitor SET name = "Monitor", description = "4K Monitor", price = 399.99;
CREATE product:headphones SET name = "Headphones", description = "Noise-cancelling headphones", price = 149.99;
CREATE product:usb_hub SET name = "USB Hub", description = "7-port USB hub", price = 39.99;
CREATE product:external_ssd SET name = "External SSD", description = "1TB external SSD", price = 129.99;


DEFINE TABLE warehouse SCHEMAFULL
    PERMISSIONS
        FOR select, create, update, delete WHERE $auth.id != NONE;

DEFINE FIELD name ON warehouse TYPE string;
DEFINE FIELD location ON warehouse TYPE string;
DEFINE FIELD created_at ON warehouse TYPE datetime DEFAULT time::now();

CREATE warehouse:main SET name = "Main Warehouse", location = "Berlin";
CREATE warehouse:secondary SET name = "Secondary Warehouse", location = "Munich";
CREATE warehouse:distribution SET name = "Distribution Center", location = "Frankfurt";


DEFINE TABLE inventory SCHEMAFULL
    PERMISSIONS
        FOR select, create, update, delete WHERE $auth.id != NONE;

DEFINE FIELD product ON inventory TYPE record<product>;
DEFINE FIELD warehouse ON inventory TYPE record<warehouse>;
DEFINE FIELD quantity ON inventory TYPE int;
DEFINE FIELD created_at ON inventory TYPE datetime DEFAULT time::now();

CREATE inventory:1 SET product = product:book, warehouse = warehouse:main, quantity = 150;
CREATE inventory:2 SET product = product:book, warehouse = warehouse:secondary, quantity = 75;
CREATE inventory:3 SET product = product:laptop, warehouse = warehouse:main, quantity = 20;
CREATE inventory:4 SET product = product:laptop, warehouse = warehouse:distribution, quantity = 35;
CREATE inventory:5 SET product = product:mouse, warehouse = warehouse:main, quantity = 500;
CREATE inventory:6 SET product = product:mouse, warehouse = warehouse:secondary, quantity = 300;
CREATE inventory:7 SET product = product:mouse, warehouse = warehouse:distribution, quantity = 200;
CREATE inventory:8 SET product = product:keyboard, warehouse = warehouse:main, quantity = 80;
CREATE inventory:9 SET product = product:keyboard, warehouse = warehouse:secondary, quantity = 60;
CREATE inventory:10 SET product = product:monitor, warehouse = warehouse:main, quantity = 45;
CREATE inventory:11 SET product = product:monitor, warehouse = warehouse:distribution, quantity = 30;
CREATE inventory:12 SET product = product:headphones, warehouse = warehouse:secondary, quantity = 120;
CREATE inventory:13 SET product = product:usb_hub, warehouse = warehouse:main, quantity = 200;
CREATE inventory:14 SET product = product:usb_hub, warehouse = warehouse:distribution, quantity = 150;
CREATE inventory:15 SET product = product:external_ssd, warehouse = warehouse:main, quantity = 90;
CREATE inventory:16 SET product = product:external_ssd, warehouse = warehouse:secondary, quantity = 110;


DEFINE TABLE orders SCHEMAFULL
    PERMISSIONS
        FOR select, create, update, delete WHERE $auth.id != NONE;

DEFINE FIELD user ON orders TYPE record<user>;
DEFINE FIELD products ON orders TYPE array<record<product>>;
DEFINE FIELD total ON orders TYPE float;
DEFINE FIELD created_at ON orders TYPE datetime DEFAULT time::now();

CREATE orders:1 SET user = user:alice, products = [product:book], total = 19.99;
CREATE orders:2 SET user = user:bob, products = [product:book, product:laptop], total = 1019.98;
CREATE orders:3 SET user = user:charlie, products = [product:mouse, product:keyboard], total = 119.98;
CREATE orders:4 SET user = user:diana, products = [product:monitor], total = 399.99;
CREATE orders:5 SET user = user:alice, products = [product:headphones, product:usb_hub], total = 189.98;
CREATE orders:6 SET user = user:eve, products = [product:external_ssd, product:mouse], total = 159.98;
CREATE orders:7 SET user = user:frank, products = [product:laptop, product:monitor, product:keyboard], total = 1489.97;
CREATE orders:8 SET user = user:bob, products = [product:book, product:headphones], total = 169.98;
CREATE orders:9 SET user = user:charlie, products = [product:usb_hub, product:external_ssd], total = 169.98;
CREATE orders:10 SET user = user:diana, products = [product:keyboard, product:mouse, product:book], total = 139.97;