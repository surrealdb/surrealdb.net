DEFINE TABLE datetime SCHEMAFULL;

DEFINE FIELD name ON datetime TYPE string;
DEFINE FIELD value ON datetime TYPE option<datetime>;

CREATE datetime SET name = 'none', value = NONE;
CREATE datetime SET name = "time", value = d"2022-07-03T07:18:52Z";
CREATE datetime SET name = "nano", value = d"2022-07-03T07:18:52.841147Z";
CREATE datetime SET name = "timezone", value = d"2022-07-03T07:18:52.841147+02:00";
CREATE datetime SET name = "time+duration", value = d"2022-07-03T07:18:52Z" + 2w;
CREATE datetime SET name = "nano+duration", value = d"2022-07-03T07:18:52.841147Z" + 1h30m20s1350ms;
CREATE datetime SET name = "full-nano", value = d"2022-07-03T07:18:52.123456789Z";