DEFINE TABLE duration SCHEMAFULL;

DEFINE FIELD name ON duration TYPE string;
DEFINE FIELD value ON duration TYPE option<duration>;

CREATE duration SET name = 'none', value = NONE;
CREATE duration SET name = 'nanosecond', value = 2ns;
CREATE duration SET name = 'microsecond', value = 3µs;
CREATE duration SET name = 'microsecond-alias', value = 4µs; // TODO : 4us
CREATE duration SET name = 'millisecond', value = 50ms;
CREATE duration SET name = 'second', value = 7s;
CREATE duration SET name = 'minute', value = 5m;
CREATE duration SET name = 'hour', value = 1h;
CREATE duration SET name = 'day', value = 6d;
CREATE duration SET name = 'week', value = 28w;
CREATE duration SET name = 'year', value = 12y;
CREATE duration SET name = 'complex', value = 1h30m20s1350ms;