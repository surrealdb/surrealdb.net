-- tables
DEFINE TABLE column SCHEMALESS;

DEFINE FIELD name ON column TYPE string;
DEFINE FIELD tasks ON column TYPE set<record<task>>
    DEFAULT [];
DEFINE FIELD order ON column TYPE int;
DEFINE FIELD created_at ON column TYPE datetime
    VALUE $before OR $value
    DEFAULT time::now();

DEFINE TABLE task SCHEMALESS;

DEFINE FIELD title ON task TYPE string;
DEFINE FIELD due_date ON task TYPE datetime;
DEFINE FIELD created_at ON task TYPE datetime
    VALUE $before OR $value
    DEFAULT time::now();
DEFINE FIELD deleted ON task TYPE bool
    DEFAULT false;

-- events
DEFINE TABLE create_task SCHEMAFULL;

DEFINE FIELD title ON create_task TYPE string;
DEFINE FIELD due_date ON create_task TYPE datetime;
DEFINE FIELD column ON create_task TYPE record<column>;

DEFINE EVENT create_task ON TABLE create_task WHEN $event = "CREATE" THEN {
    LET $task = (CREATE task SET title = $value.title, due_date = $value.due_date);

    UPDATE column
    MERGE { tasks: array::concat(tasks, $task.id) }
    WHERE id = $value.column;
};

DEFINE TABLE delete_task SCHEMAFULL;

DEFINE FIELD task ON delete_task TYPE record<task>;

DEFINE EVENT delete_task ON TABLE delete_task WHEN $event = "CREATE" THEN {
    UPDATE task SET deleted = true WHERE id = $value.task;

    UPDATE column
    MERGE { tasks: array::difference(tasks, [$value.task]) }
    WHERE $value.task IN tasks;
};

DEFINE TABLE move_task SCHEMAFULL;

DEFINE FIELD task ON move_task TYPE record<task>;
DEFINE FIELD from ON move_task TYPE record<column>;
DEFINE FIELD to ON move_task TYPE record<column>;
DEFINE FIELD new_index ON move_task TYPE int;

DEFINE EVENT move_task ON TABLE move_task WHEN $event = "CREATE" THEN {
    UPDATE column
    MERGE { tasks: array::difference(tasks, [$value.task]) }
    WHERE id = $value.from;

    UPDATE column
    MERGE { tasks: array::insert(tasks, $value.task, $value.new_index) }
    WHERE id = $value.to;
};

-- views
DEFINE TABLE displayable_column AS
SELECT
    id,
    name,
    order,
    tasks.*.*
FROM column