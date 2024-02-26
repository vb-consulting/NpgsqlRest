# NpgsqlRest.CrudSource

**Data source plug-in** for the `NpgsqlRest` library. 

It provides data source access to PostgreSQL tables and views to create CRUD endpoints:

- `GET table` for the `SELECT` operations.
- `POST table` for the `UPDATE` operations.
- `POST table/returning` for the `UPDATE RETURNING` operations. 
- `DELETE table` for the `DELETE` operations.
- `DELETE table/returning` for the `DELETE RETURNING` operations.
- `PUT table` for the `INSERT` operations.
- `PUT table/returning` for the `INSERT RETURNING` operations.
- `PUT table/on-conflict-do-nothing` for the `INSERT ON CONFLICT DO NOTHING` operations.
- `PUT table/on-conflict-do-nothing/returning` for the `INSERT ON CONFLICT DO NOTHING RETURING` operations.
- `PUT table/on-conflict-do-update` for the `INSERT ON CONFLICT DO UPDATE` operations.
- `PUT table/on-conflict-do-update/returning` for the `INSERT ON CONFLICT DO NOTHING UPDATE` operations.

## Example

Following table:

```sql
create table my_crud_table (
    id int primary key,
    name text
);
```

Given that the default name converter is used, and all settings are default, it will produce the following endpoints with the following parameters:

### 1) GET /api/my-crud-table/

Executes the following query:

```sql
select
    id, name
from 
    public.my_crud_table
where
    id = $1 and name = $2
```

All parameters are optional and supplied as the query string: `/api/my-crud-table/?id=1&name=name`.

- Parameter `$1` maps to the `id` query string optional parameter.
- Parameter `$2` maps to the `name` query string optional parameter.

The endpoint returns the status `200 OK` with the `application/json` type content containing JSON array with the query results.

Tags for this endpoint are `select`, `read` and `get`.

### 2) POST /api/my-crud-table/

Executes the following query:

```sql
update public.my_crud_table
set
    name = $1
where
    id = $2
```

- All parameters are optional, except that there must be at least one for the primary key and one for not primary key.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

- All parameters that map to the primary key field are going to the "where" part and non-primary key parameters go to the "set" part.
- If there are no parameters that map to the primary key fields, the endpoint is `404 NotFound`.
- If there are no parameters that don't map to primary key fields, the endpoint is `404 NotFound`.

Otherwise, the successful execution returns `204 NoContent` without content.

Tags for this endpoint are `update` and `post`.

### 3) POST /api/my-crud-table/returning/

Executes the following query:

```sql
update public.my_crud_table
set
    name = $1
where
    id = $2
returning
    id, name
```

- All parameters are optional, except that there must be at least one for the primary key and one for not primary key.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

- All parameters that map to the primary key field are going to the "where" part and non-primary key parameters go to the "set" part.
- If there are no parameters that map to the primary key fields, the endpoint is `404 NotFound`.
- If there are no parameters that don't map to primary key fields, the endpoint is `404 NotFound`.

Otherwise, the successful execution returns status `200 OK` with the `application/json` type content containing JSON array with the updated records.

Tags for this endpoint are `update`, `post`, `UpdateReturning`, `update-returning`, `update_returning` and `returning`.

### 4) DELETE /api/my-crud-table/

Executes the following query:

```sql
delete from 
    public.my_crud_table
where
    id = $1 and name = $2
```

All parameters are optional and supplied as the query string: `/api/my-crud-table/?id=1&name=name`.

- Parameter `$1` maps to the `id` query string optional parameter.
- Parameter `$2` maps to the `name` query string optional parameter.

The endpoint returns the status `204 NoContent` without content.

Tags for this endpoint is just `delete`.

### 5) DELETE /api/my-crud-table/returning/

Executes the following query:

```sql
delete from 
    public.my_crud_table
where
    id = $1 and name = $2
returning
    id, name
```

All parameters are optional and supplied as the query string: `/api/my-crud-table/?id=1&name=name`.

- Parameter `$1` maps to the `id` query string optional parameter.
- Parameter `$2` maps to the `name` query string optional parameter.

The endpoint returns the status `200 OK` with the `application/json` type content containing JSON array with the deleted records.

Tags for this endpoint are `delete`, `DeleteReturning`, `delete-returning`, `delete_returning` and `returning`.

### 6) PUT /api/my-crud-table/

Executes the following query:

```sql
insert into public.my_crud_table
(id, name)
values
($1, $2)
```

- All parameters are optional, except that there must be at least one.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

If there are no parameters the endpoint is `404 NotFound`.

Otherwise, the successful execution returns status `204 NoContent` without content.

Tags for this endpoint are `insert`, `put` and `create`.

### 7) PUT /api/my-crud-table/returning/

Executes the following query:

```sql
insert into public.my_crud_table
(id, name)
values
($1, $2)
returning
    id, name
```

- All parameters are optional, except that there must be at least one.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

If there are no parameters the endpoint is `404 NotFound`.

Otherwise, the successful execution returns status `200 OK` with the `application/json` type content containing JSON array with the inserted record.

Tags for this endpoint are `insert`, `put`, `create`, `InsertReturning`, `insert-returning`, `insert_returning` and `returning`.

### 8) PUT /api/my-crud-table/on-conflict-do-nothing/

Executes the following query:

```sql
insert into public.my_crud_table
(id, name)
values
($1, $2)
on conflict (id) do nothing
```

- All parameters are optional, except parameters that are mapped to primary key fields.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

- If there are no parameters the endpoint is `404 NotFound`.
- If parameters mapped to primary keys are missing endpoint is `404 NotFound`.

Otherwise, the successful execution returns status `204 NoContent` without content.

Tags for this endpoint are `insert`, `put`, `create`, `InsertOnConflictDoNothing`, `insert-on-conflict-do-nothing`, `insert_on_conflict_do_nothing`, `OnConflictDoNothing`, `on-conflict-do-nothing` and `on_conflict_do_nothing`.

### 9) PUT /api/my-crud-table/on-conflict-do-nothing/returning/

Executes the following query:

```sql
insert into public.my_crud_table
(id, name)
values
($1, $2)
on conflict (id) do nothing
returning
    id, name
```

- All parameters are optional, except parameters that are mapped to primary key fields.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

- If there are no parameters the endpoint is `404 NotFound`.
- If parameters mapped to primary keys are missing endpoint is `404 NotFound`.

Otherwise, the successful execution returns status `200 OK` with the `application/json` type content containing JSON array with the inserted record.

Tags for this endpoint are `insert`, `put`, `create`, `InsertOnConflictDoNothingReturning`, `insert-on-conflict-do-nothing-returning`, `insert_on_conflict_do_nothing-returning`, `OnConflictDoNothing`, `on-conflict-do-nothing`, `on_conflict_do_nothing` and `returning`.

### 10) PUT /api/my-crud-table/on-conflict-do-update/

Executes the following query:

```sql
insert into public.my_crud_table
(id, name)
values
($1, $2)
on conflict (id) do update
    id = excluded.id,
    name = excluded.name
```

- All parameters are optional, except parameters that are mapped to primary key fields.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

- If there are no parameters the endpoint is `404 NotFound`.
- If parameters mapped to primary keys are missing endpoint is `404 NotFound`.

Otherwise, the successful execution returns status `204 NoContent` without content.

Tags for this endpoint are `insert`, `put`, `create`, `InsertOnConflictDoUpdate`, `insert-on-conflict-do-update`, `insert_on_conflict_do_update`, `OnConflictDoUpdate`, `on-conflict-do-update` and `on_conflict_do_update`.

### 9) PUT /api/my-crud-table/on-conflict-do-update/returning/

Executes the following query:

```sql
insert into public.my_crud_table
(id, name)
values
($1, $2)
on conflict (id) do update
    id = excluded.id,
    name = excluded.name
```

- All parameters are optional, except parameters that are mapped to primary key fields.
- Parameters are supplied as JSON body:
  
```json
{
    "id": 1,
    "name": "name"
}
```

- If there are no parameters the endpoint is `404 NotFound`.
- If parameters mapped to primary keys are missing endpoint is `404 NotFound`.

Otherwise, the successful execution returns status `200 OK` with the `application/json` type content containing JSON array with the inserted record.

Tags for this endpoint are `insert`, `put`, `create`, `InsertOnConflictDoUpdateReturning`, `insert-on-conflict-do-update-returning`, `insert_on_conflict_do_update-returning`, `OnConflictDoUpdate`, `on-conflict-do-update`, `on_conflict_do_update` and `returning`.

## Install 

```console
dotnet add package NpgsqlRest.CrudSource --version 1.0.0
```

## Minimal Usages

Initialize `SourcesCreated` callback function that receives an initialized list of sources to add `CrudSource` source:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    SourcesCreated = sources =>
    {
        sources.Add(new CrudSource());
    },
});
app.Run();
```

To run only `CrudSource`, clear all others:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    SourcesCreated = sources =>
    {
        sources.Clear();
        sources.Add(new CrudSource());
    },
});
app.Run();
```

To run initialize `CrudSource`, only for `SELECT` and `DELETE RETURNING`, for example, use the `CrudTypes` flag:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    SourcesCreated = sources =>
    {
        sources.Add(new CrudSource
        {
            CrudTypes = CrudCommandType.Select | CrudCommandType.DeleteReturning
        });
    },
});
app.Run();
```

To run initialize `CrudSource`, only for tables `my_crud_table` and `other_table`, you can use the `IncludeNames` array property:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    SourcesCreated = sources =>
    {
        sources.Add(new CrudSource
        {
            IncludeNames = ["my_crud_table", "other_table"],
        });
    },
});
app.Run();
```

To run initialize `CrudSource`, only for tables containing a pattern, use the `NameSimilarTo` or `NameNotSimilarTo` properties. For example, the name starts with `crud`:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    SourcesCreated = sources =>
    {
        sources.Add(new CrudSource
        {
            NameSimilarTo = "crud%",
        });
    },
});
app.Run();
```

To fine-tune what routines and what types you need, use the `Created` callback and return `true` or `false` to disable or enable:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    SourcesCreated = sources =>
    {
        Created = (routine, type) =>
        {
            if (routine.Name == "my_crud_table")
            {
                return type switch
                {
                    CrudCommandType.Select => true,
                    CrudCommandType.DeleteReturning => true,
                    _ => false
                };
            }
            return true;
        }
    },
});
app.Run();
```

## Options

| Option | Default | Description |
| ------ | ------- | ----------- |
| `string? SchemaSimilarTo` | `null` | When not NULL, overrides the main option `SchemaSimilarTo`. It filters schemas similar to this or null to ignore this parameter. |
| `string? SchemaNotSimilarTo` | `null` | When not NULL, overrides the main option `SchemaNotSimilarTo`. It filters schemas not similar to this or null to ignore this parameter. |
| `string[]? IncludeSchemas` | `null` | When not NULL, overrides the main option `IncludeSchemas`. List of schema names to be included or null to ignore this parameter. |
| `string[]? ExcludeSchemas` | `null` | When not NULL, overrides the main option `ExcludeSchemas`. List of schema names to be excluded or null to ignore this parameter. |
| `string? NameSimilarTo` | `null` | When not NULL, overrides the main option `NameSimilarTo`. It filters names similar to this or null to ignore this parameter. |
| `string? NameNotSimilarTo` | `null` | When not NULL, overrides the main option `NameNotSimilarTo`. It filters names not similar to this or null to ignore this parameter. |
| `string[]? IncludeNames` | `null` | When not NULL, overrides the main option `IncludeNames`. List of names to be included or null to ignore this parameter. |
| `string[]? ExcludeNames` | `null` | When not NULL, overrides the main option `ExcludeNames`. List of names to be excluded or null to ignore this parameter. |
| `string Query` | `null` | Custom query to return list of tables and views or null to use the default. |
| `CrudCommandType CrudTypes` | `CrudCommandType.All` | Type of CRUD queries and commands to create. |
| `string ReturningUrlPattern` | `"{0}/returning"` | URL pattern for all "returning" endpoints. Parameter is the original URL. |
| `string OnConflictDoNothingUrlPattern` | `"{0}/on-conflict-do-nothing"` | URL pattern for all "do nothing" endpoints. Parameter is the original URL. |
| `string OnConflictDoNothingReturningUrlPattern` | `"{0}/on-conflict-do-nothing/returning"` | URL pattern for all "do nothing returning " endpoints. Parameter is the original URL. |
| `string OnConflictDoUpdateUrlPattern` | `"{0}/on-conflict-do-update"` | URL pattern for all "do update" endpoints. Parameter is the original URL. |
| `string OnConflictDoUpdateReturningUrlPattern` | `"{0}/on-conflict-do-update/returning"` | URL pattern for all "do update returning" endpoints. Parameter is the original URL. |
| `Func<Routine, CrudCommandType, bool>? Created` | `null` | Callback function, when not null it is evaluated when Routine object is created for a certain type. Return true or false to disable or enable endpoints. |
| `CommentsMode? CommentsMode` | `null` | Comments mode (`Ignore`, `ParseAll`, `OnlyWithHttpTag`), when not null overrides the `CommentsMode` from the main options. |

#### Library Dependencies

- NpgsqlRest 2.0.0

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
