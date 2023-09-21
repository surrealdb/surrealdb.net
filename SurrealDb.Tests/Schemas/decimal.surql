DEFINE TABLE decimal SCHEMAFULL;

DEFINE FIELD name ON decimal TYPE string;
DEFINE FIELD value ON decimal TYPE option<number>;

CREATE decimal SET name = 'none', value = NONE;
CREATE decimal SET name = 'min', value = -9223372036854775808;
CREATE decimal SET name = 'max', value = 9223372036854775807;
CREATE decimal SET name = 'zero', value = 0;
CREATE decimal SET name = 'decimal', value = 41.5dec;
CREATE decimal SET name = 'decimal-precision', value = 13.5719384719384719385639856394139476937756394756dec;
CREATE decimal SET name = 'float', value = <float> 13.5719384719384719385639856394139476937756394756;