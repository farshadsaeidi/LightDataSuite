# 🚀 LightDataSuite

LightDataSuite is a lightweight framework for executing queries against SQL databases using modern **GraphQL** and **KQL** (Kusto Query Language) syntax. It acts as a high-performance translator, converting frontend-friendly queries directly into secure, parameterized T-SQL.

---

## 💡 Why This is Important for the Frontend

Standard REST APIs often lead to **Over-fetching** (getting more data than you need) or **Under-fetching** (making multiple calls to get related data). 

* **GraphQL** solves this by letting the frontend request a specific "tree" of data in one go.
* **KQL** provides a readable "pipeline" flow (`|`), making it perfect for dynamic dashboards, filtering, and data transformation without complex backend changes.



---

## 🛠️ Engine Components

### 1. BaseDataEngine.cs
The core security layer of the suite. It manages:
* **Connection Handling:** Robust management of SQL connections.
* **Validation:** Strict whitelisting of tables and blocking of unauthorized fields.
* **Safety:** Full support for SQL parameter binding to prevent injection.

### 2. GraphQLEngine.cs
Implements `IGraphQLEngine` to translate GraphQL AST into SQL.
* **Nested Selections:** Fetch parent and child records in a single SQL `FOR JSON` query.
* **Arguments:** Built-in support for `first` (Top), `order`, and `desc`.
* **Flexible Joins:** Use `on` and `parentkey` arguments to define relationships dynamically.

### 3. KqlEngine.cs
Implements `IKqlEngine` for a pipeline-based query experience.
* **Pipeline Operators:** Supports `where`, `take`, `project`, and `order by`.
* **Aliasing:** Easily rename columns on the fly (e.g., `project NewName = OldName`).
* **Nested Projections:** Supports complex column selections and calculations.

---

## ⌨️ Usage & Samples

### Initialization
```csharp
// Define your security rules
string allowedTables = "users,orders,products";
string blockedFields = "password,hashtoken";

var gqlEngine = new GraphQLEngine(connectionString, allowedTables, blockedFields);
var kqlEngine = new KqlEngine(connectionString, allowedTables, blockedFields);
```

### GraphQL Sample
**Input JSON:**
```json
{
  "query": "{ customers(city: $city) { name, orders(on: 'customerid', parentkey: 'id') { total } } }",
  "city": "london"
}
```

### KQL Sample
**Input JSON:**
```json
{
  "query": "products | where price > 100 | project name, price | take 5"
}
```
*All results are returned as a JSON string via SQL's `FOR JSON PATH`.*

---

## ⚖️ How This Differs from "Standard" Versions

This suite is a **Lightweight Transpiler**, not a full-scale server implementation.

| Feature | LightDataSuite | Standard GraphQL/KQL |
| :--- | :--- | :--- |
| **Translation** | Directly to **T-SQL** | Resolvers or Kusto Engine |
| **Casing** | **Case-Insensitive** (forced lowercase) | Strictly Case-Sensitive |
| **Infrastructure** | Zero-Config (SQL Only) | Requires specialized servers |
| **Security** | Field/Table Whitelisting | Complex RBAC/Schemas |

---

## ✨ Key Features

* **Normalization:** Automatically lowercases queries for consistency across different frontend environments.
* **Security First:** Every query is parameterized. No raw strings are passed to the database.
* **Performance:** Utilizes `FOR JSON PATH` in SQL Server for extremely fast data serialization.
* **Configurable:** Easily restrict access to sensitive tables or specific columns (like passwords or internal IDs).

---

## 🛡️ Security Best Practices
1. **Never** hardcode values in the query string; pass them as JSON properties to use the automatic parameterization.
2. **Always** define your `allowedTables` and `blockedFields` during engine initialization.
3. These engines are **Read-Only**. They are designed strictly for data retrieval.
