# NpgsqlRest.TsClient

**Automatic Typescript Client Code Generation for NpgsqlRest**

**Metadata plug-in** for the `NpgsqlRest` library. 

Provides support for the generation of the **[Typescript files]** with interfaces and fetch functions.

The generated Typescript module can be re-generated on every build which effectively gives a **static type-checking of your database**.

## Install 

```console
dotnet add package NpgsqlRest.TsClient --version 1.0.0
```

## Example

### Usage

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreateHandlers = [
        //
        // Configure TsClient Code Gen
        //
        new TsClient(new TsClientOptions
        {
            // Frontend file path
            FilePath = "../Frontend/src/api.ts",
            // Always overwrite
            FileOverwrite = true,
            // Include full host name in URL
            IncludeHost = true,
        }),
    ],

    // add also CRUD plugin
    SourcesCreated = sources => sources.Add(new CrudSource())
});
```

#### Database

```sql
create table customers (
  customer_id bigint not null PRIMARY KEY, 
  name text NOT NULL, 
  email text NULL, 
  created_at TIMESTAMP NOT NULL default now()
);

create function get_latest_customer() 
returns customers 
language sql 
as $$
select * 
from customers
order by created_at dec
limit 1
$$;

create function get_duplicate_email_customers() returns setof customers language sql 
as $$
select 
    customer_id, name, email, created_at
from (
    select 
        customer_id, name, email, created_at, row_number() over(partition by email) as occurance
    from customers
)
where occurance > 1
$$;

create function get_customers_count(
    _name text, 
    _email text, 
    _created_before timestamp
) 
returns bigint language sql as $$
select 
    count(*)
from customers
where 
    (_name is null or name ilike _name)
    and
    (_email is null or email ilike _email)
    and
    (_created_before is null or created_at > _created_before)
$$;

create function get_duplicate_emails() 
returns table (email text, count bigint) language sql as $$
select 
    email, count(*)
from customers
group by email
having count(*) > 1
$$;
```

#### Generated Typescript

```ts
interface ICustomersGetRequest {
    customerId?: number | null;
    name?: string | null;
    email?: string | null;
    createdAt?: Date | null;
}

interface ICustomersGetResponse {
    customerId: number | null;
    name: string | null;
    email: string | null;
    createdAt: string | null;
}

interface ICustomersPutRequest {
    customerId: number | null;
    name: string | null;
    email?: string | null;
    createdAt?: Date | null;
}

interface IGetCustomersCountRequest {
    name: string | null;
    email: string | null;
    createdBefore: Date | null;
}

interface IGetDuplicateEmailsResponse {
    email: string | null;
    count: number | null;
}

const _baseUrl = "";

const _parseQuery = (query: Record<any, any>) => "?" + Object.keys(query)
    .map(key => {
        const value = query[key] ? query[key] : "";
        if (Array.isArray(value)) {
            return value.map(s => s ? `${key}=${encodeURIComponent(s)}` : `${key}=`).join("&");
        }
        return `${key}=${encodeURIComponent(value)}`;
    })
    .join("&");

/**
* select public.customers
* 
* @remarks
* GET /api/customers
* 
* @see TABLE public.customers
*/
export async function customersGet(request: ICustomersGetRequest) : Promise<ICustomersGetResponse[]> {
    const response = await fetch(_baseUrl + "/api/customers" + _parseQuery(request), {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return await response.json() as ICustomersGetResponse[];
}

/**
* update public.customers
* 
* @remarks
* POST /api/customers
* 
* @see TABLE public.customers
*/
export async function customersPost(request: ICustomersGetRequest) : Promise<void> {
    await fetch(_baseUrl + "/api/customers", {
        method: "POST",
        body: JSON.stringify(request)
    });
}

/**
* update public.customers
* returning
*     customer_id, name, email, created_at
* 
* @remarks
* POST /api/customers/returning
* 
* @see TABLE public.customers
*/
export async function customersReturningPost(request: ICustomersGetRequest) : Promise<ICustomersGetResponse[]> {
    const response = await fetch(_baseUrl + "/api/customers/returning", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request)
    });
    return await response.json() as ICustomersGetResponse[];
}

/**
* delete from public.customers
* 
* @remarks
* DELETE /api/customers
* 
* @see TABLE public.customers
*/
export async function customersDelete(request: ICustomersGetRequest) : Promise<void> {
    await fetch(_baseUrl + "/api/customers" + _parseQuery(request), {
        method: "DELETE",
    });
}

/**
* delete from public.customers
* returning
*     customer_id, name, email, created_at
* 
* @remarks
* DELETE /api/customers/returning
* 
* @see TABLE public.customers
*/
export async function customersReturningDelete(request: ICustomersGetRequest) : Promise<ICustomersGetResponse[]> {
    const response = await fetch(_baseUrl + "/api/customers/returning" + _parseQuery(request), {
        method: "DELETE",
        headers: { "Content-Type": "application/json" },
    });
    return await response.json() as ICustomersGetResponse[];
}

/**
* insert into public.customers
* 
* @remarks
* PUT /api/customers
* 
* @see TABLE public.customers
*/
export async function customersPut(request: ICustomersPutRequest) : Promise<void> {
    await fetch(_baseUrl + "/api/customers", {
        method: "PUT",
        body: JSON.stringify(request)
    });
}

/**
* insert into public.customers
* returning
*     customer_id, name, email, created_at
* 
* @remarks
* PUT /api/customers/returning
* 
* @see TABLE public.customers
*/
export async function customersReturningPut(request: ICustomersGetRequest) : Promise<ICustomersGetResponse[]> {
    const response = await fetch(_baseUrl + "/api/customers/returning", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request)
    });
    return await response.json() as ICustomersGetResponse[];
}

/**
* insert into public.customers
* on conflict (customer_id) do nothing
* 
* @remarks
* PUT /api/customers/on-conflict-do-nothing
* 
* @see TABLE public.customers
*/
export async function customersOnConflictDoNothingPut(request: ICustomersPutRequest) : Promise<void> {
    await fetch(_baseUrl + "/api/customers/on-conflict-do-nothing", {
        method: "PUT",
        body: JSON.stringify(request)
    });
}

/**
* insert into public.customers
* on conflict (customer_id) do nothing
* returning
*     customer_id, name, email, created_at
* 
* @remarks
* PUT /api/customers/on-conflict-do-nothing/returning
* 
* @see TABLE public.customers
*/
export async function customersOnConflictDoNothingReturningPut(request: ICustomersPutRequest) : Promise<ICustomersGetResponse[]> {
    const response = await fetch(_baseUrl + "/api/customers/on-conflict-do-nothing/returning", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request)
    });
    return await response.json() as ICustomersGetResponse[];
}

/**
* insert into public.customers
* on conflict (customer_id) do update
* 
* @remarks
* PUT /api/customers/on-conflict-do-update
* 
* @see TABLE public.customers
*/
export async function customersOnConflictDoUpdatePut(request: ICustomersPutRequest) : Promise<void> {
    await fetch(_baseUrl + "/api/customers/on-conflict-do-update", {
        method: "PUT",
        body: JSON.stringify(request)
    });
}

/**
* insert into public.customers
* on conflict (customer_id) do update
* returning
*     customer_id, name, email, created_at
* 
* @remarks
* PUT /api/customers/on-conflict-do-update/returning
* 
* @see TABLE public.customers
*/
export async function customersOnConflictDoUpdateReturningPut(request: ICustomersPutRequest) : Promise<ICustomersGetResponse[]> {
    const response = await fetch(_baseUrl + "/api/customers/on-conflict-do-update/returning", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request)
    });
    return await response.json() as ICustomersGetResponse[];
}

/**
* function public.get_customers_count(
*     _name text,
*     _email text,
*     _created_before timestamp without time zone
* )
* returns bigint
* 
* @remarks
* GET /api/get-customers-count
* 
* @see FUNCTION public.get_customers_count
*/
export async function getCustomersCount(request: IGetCustomersCountRequest) : Promise<number> {
    const response = await fetch(_baseUrl + "/api/get-customers-count" + _parseQuery(request), {
        method: "GET",
    });
    return Number(await response.text());
}

/**
* function public.get_duplicate_email_customers()
* returns table(
*     customer_id bigint,
*     name text,
*     email text,
*     created_at timestamp without time zone
* )
* 
* @remarks
* GET /api/get-duplicate-email-customers
* 
* @see FUNCTION public.get_duplicate_email_customers
*/
export async function getDuplicateEmailCustomers() : Promise<ICustomersGetResponse[]> {
    const response = await fetch(_baseUrl + "/api/get-duplicate-email-customers", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return await response.json() as ICustomersGetResponse[];
}

/**
* function public.get_duplicate_emails()
* returns table(
*     email text,
*     count bigint
* )
* 
* @remarks
* GET /api/get-duplicate-emails
* 
* @see FUNCTION public.get_duplicate_emails
*/
export async function getDuplicateEmails() : Promise<IGetDuplicateEmailsResponse[]> {
    const response = await fetch(_baseUrl + "/api/get-duplicate-emails", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return await response.json() as IGetDuplicateEmailsResponse[];
}

/**
* function public.get_latest_customer()
* returns customers
* 
* @remarks
* GET /api/get-latest-customer
* 
* @see FUNCTION public.get_latest_customer
*/
export async function getLatestCustomer() : Promise<ICustomersGetResponse> {
    const response = await fetch(_baseUrl + "/api/get-latest-customer", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return await response.json() as ICustomersGetResponse;
}
```

## Options

```csharp
public class TsClientOptions(
    string filePath = default!,
    bool fileOverwrite = false,
    bool includeHost = false,
    string? customHost = null,
    CommentHeader commentHeader = CommentHeader.Simple)
{
    /// <summary>
    /// File path for the generated code. Set to null to skip the code generation.
    /// </summary>
    public string? FilePath { get; set; } = filePath;
    /// <summary>
    /// Force file overwrite.
    /// </summary>
    public bool FileOverwrite { get; set; } = fileOverwrite;
    /// <summary>
    /// Include current host information in the URL prefix.
    /// </summary>
    public bool IncludeHost { get; set; } = includeHost;
    /// <summary>
    /// Set the custom host prefix information.
    /// </summary>
    public string? CustomHost { get; set; } = customHost;
    /// <summary>
    /// Adds comment header to above request based on PostgreSQL routine
    /// Set None to skip.
    /// Set Simple (default) to add name, parameters and return values to comment header.
    /// Set Full to add the entire routine code as comment header.
    /// </summary>
    public CommentHeader CommentHeader { get; set; } = commentHeader;
    /// <summary>
    /// Set to true to include status code in response: {status: response.status, response: model}
    /// </summary>
    public bool IncludeStatusCode { get; set; } = includeStatusCode;
}
```

## Changelog

### 1.1.0

Added `IncludeStatusCode` option. When set to true, the generated function includes a status code. Example:

```ts
export async function getDuplicateEmailCustomers() : Promise<{status: number, response: IGetDuplicateEmailCustomersResponse[]}> {
    const response = await fetch(_baseUrl + "/api/get-duplicate-email-customers", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return {status: response.status, response: await response.json() as IGetDuplicateEmailCustomersResponse[]};
}
```

## Library Dependencies

- NpgsqlRest 2.0.0

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
