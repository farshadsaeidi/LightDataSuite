# 🔷 LightDataSuite

A lightweight, secure data engine framework for **ASP.NET Core** that lets you query **SQL Server** using a **GraphQL-like** or **KQL-like** syntax — without exposing raw SQL to clients.

---

## 🏗️ Architecture
![Alt text](relative/path/to/diagram.png)

---

## 📁 Project Structure

| File | Role |
|------|------|
| `EngineConfig.cs` | Centralized config: connection string, allowed tables, blocked fields, hard limits |
| `BaseDataEngine.cs` | Abstract base: table/field validation, parameterized SQL execution, limit enforcement |
| `KqlEngine.cs` | Parses KQL pipeline queries → validated → SQL |
| `GraphQLEngine.cs` | Parses GraphQL-style queries → validated → SQL |
| `Program.cs` | ASP.NET Core DI setup, CORS, `/KQL` and `/GQL` endpoints |
| `test.ts` | TypeScript client: `loadKqlData()` and `loadGraphData()` fetch helpers |

---

## 🚀 Quick Start

### 1. Configure `Program.cs`

```csharp
var config = new EngineConfig
{
    // Required — your SQL Server connection string
    ConnectionString = "Data Source=.;Initial Catalog=MyDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;",

    // Required — pipe-separated table whitelist
    AllowedTables = "MasterAccounts|subaccounts|Invoices",

    // Required — pipe-separated sensitive field blacklist
    BlockedFields = "Password|OTP|Pin",

    // You can change these if you need something other than the defaults
    // MaxNestedLevel  = 3,        // Max subquery nesting depth
    // MaxRows         = 100,      // Max rows returned per query
    // MaxFields       = 30,       // Max fields across entire query
    // MaxParameters   = 10,       // Max SQL parameters per request
    // MaxJsonLength   = 262144,   // 256KB max response size
    // MaxQueryLength  = 2048,     // 2KB max request payload
    // CommandTimeout  = 15        // SQL execution timeout in seconds
};

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<IKqlEngine, KqlEngine>();
builder.Services.AddSingleton<IGraphQLEngine, GraphQLEngine>();
```

### 2. Enable CORS (development)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("Development"); // Must be before endpoints!
app.UseHttpsRedirection();
```

### 3. Run

```bash
dotnet run
```

---

## 📡 Endpoints

| Method | Endpoint | Engine |
|--------|----------|--------|
| `POST` | `/KQL` | `KqlEngine` |
| `POST` | `/GQL` | `GraphQLEngine` |

Both accept `Content-Type: application/json` and return `application/json`.

---

## 🔵 KQL Usage

Uses a `|` pipeline syntax.

```json
POST /KQL
{
  "query": "MasterAccounts
            | where CustomerID == @id
            | project FirstName, LastName, CustomerID,
              Invoices = (Invoices
                | where CustomerID == MasterAccounts.CustomerID
                | order by InvoiceID desc
                | project InvoiceID, Amount, CreateDate
                | take 2
              )",
  "id": 10540
}
```

### KQL Operators

| Operator | Example | Notes |
|----------|---------|-------|
| `where` | `where CustomerID == @id` | Only `field = @param` pattern allowed |
| `take` | `take 10` | Capped by `MaxRows` |
| `project` | `project FirstName, LastName` | Select columns |
| `order by` | `order by InvoiceID` | Plain field name only |
| `alias = (...)` | `Invoices = (Invoices \| ...)` | Nested correlated subquery |

> **Parameters**: Use `@name` or `$name` as placeholders. Pass matching keys in the JSON body.

---

## 🟢 GraphQL Usage

Uses a GraphQL-style nested selection syntax.

```json
POST /GQL
{
  "query": "{ masteraccounts(customerid: $id, first: 2) { firstname lastname invoices(customerid: $id, first: 2) { invoiceid amount createdate } } }",
  "id": 1230
}
```

### GQL Arguments

| Argument | Reserved | Description |
|----------|----------|-------------|
| `first` | ✅ | Limit rows returned |
| `order` | ✅ | Field name to sort by |
| `desc` | ✅ | Sort descending (`true` / `false`) |
| any field | ❌ | Becomes a `WHERE` filter — safely parameterized |

> **Parameters**: Use `$name` as placeholder. Pass matching keys in the JSON body.

---

## 🔒 Security

| Protection | Mechanism |
|------------|-----------|
| **SQL Injection (GQL)** | All WHERE values use `SqlParameter` — never raw strings |
| **SQL Injection (KQL)** | WHERE validated against `field = @param` Regex pattern |
| **ORDER BY injection** | Only plain `^\w+$` field names allowed |
| **Table access** | `AllowedTables` whitelist — unlisted tables throw `UnauthorizedAccessException` |
| **Sensitive fields** | `BlockedFields` blacklist blocks Password, OTP, etc. |
| **Response size** | `MaxJsonLength` (256KB default) |
| **Request size** | `MaxQueryLength` (2KB default) |
| **Row count** | `MaxRows` (100 default) |
| **Field count** | `MaxFields` (30 default) |
| **Timeout** | `CommandTimeout` (15s default) |
| **Nesting depth** | `MaxNestedLevel` (3 default) |

---

## 💻 TypeScript Client

Place `test.ts` in your project and import the exported functions:

```typescript
import { loadGraphData, loadKqlData } from './test';

// GQL — fetch masteraccounts + nested invoices
const gqlResult = await loadGraphData(1230);
console.log(gqlResult);

// KQL — same data via pipeline syntax
const kqlResult = await loadKqlData(10540);
console.log(kqlResult);
```

**`test.ts` internals:**

```typescript
const API_BASE = "https://localhost:7190";

// GQL query
export const loadGraphData = async (id: number): Promise<Account[]> => {
    const request: DataRequest = {
        query: `{ masteraccounts(customerid: $id) { firstname lastname invoices(customerid: $id, first: 2) { invoiceid amount createdate } } }`,
        id: id
    };
    return fetchData("/GQL", request);
};

// KQL query
export const loadKqlData = async (id: number): Promise<Account[]> => {
    const request: DataRequest = {
        query: `masteraccounts
                | where customerid == $id
                | project firstname, lastname,
                  invoices = (invoices | where customerid == $id | take 2 | project invoiceid, amount, createdate)`,
        id: id
    };
    return fetchData("/KQL", request);
};
```

---

## 📦 NuGet Dependencies

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="*" />
<PackageReference Include="GraphQLParser" Version="*" />
```

---

## ⚠️ Production Notes

- Replace `SetIsOriginAllowed(localhost)` CORS policy with your specific production domain
- Move `ConnectionString` to `appsettings.json` or environment variables — never hardcode in source
- Set `AllowedTables` to only the tables your API needs to expose

---

## 📄 License

MIT
