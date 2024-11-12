# SurrealDb.Examples.Console

A simple Console app example using the .NET SDK for SurrealDB.

## Get started

First, start a new SurrealDB local instance:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Then make sure your SurrealDB server is running on `127.0.0.1:8000` and run your app from the command line with:

```sh
dotnet run
```

This will then output something similar to this:

```json
{
  "Title": "Founder \u0026 CEO",
  "Name": {
    "FirstName": "Tobie",
    "LastName": "Morgan Hitchcock"
  },
  "Marketing": true,
  "id": "person:zdnk39wm0vk3olv75az7"
}
{
  "Title": null,
  "Name": null,
  "Marketing": true,
  "id": "person:jaime"
}
[
  {
    "Title": null,
    "Name": null,
    "Marketing": true,
    "id": "person:jaime"
  },
  {
    "Title": "Founder \u0026 CEO",
    "Name": {
      "FirstName": "Tobie",
      "LastName": "Morgan Hitchcock"
    },
    "Marketing": true,
    "id": "person:zdnk39wm0vk3olv75az7"
  }
]
[
  {
    "Marketing": true,
    "Count": 2
  }
]
```

Open `Program.cs`, you can now tweak the code.