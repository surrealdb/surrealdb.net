DEFINE TABLE nested_object SCHEMALESS;

CREATE nested_object:array
SET
    name = "parent",
    sub_items = [
        { name: "child1" },
        { name: "child2" }
    ];
