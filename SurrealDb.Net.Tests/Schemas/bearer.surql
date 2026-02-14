DEFINE TABLE user SCHEMAFULL
    PERMISSIONS
        FOR select FULL
        FOR update WHERE id = $auth.id
        FOR create, delete NONE;

DEFINE FIELD username ON user TYPE string;
DEFINE FIELD email ON user TYPE string ASSERT string::is_email($value);
DEFINE FIELD password ON user TYPE string PERMISSIONS FOR SELECT NONE;
DEFINE FIELD registered_at ON user TYPE datetime DEFAULT time::now();

DEFINE INDEX unique_username ON user COLUMNS username UNIQUE;
DEFINE INDEX unique_email ON user COLUMNS email UNIQUE;

CREATE user:johndoe
SET username = "johndoe", email = "john.doe@example.com", password = crypto::argon2::generate("password123");

DEFINE ACCESS api
    ON DATABASE
    TYPE BEARER FOR RECORD;