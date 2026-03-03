# 🚀 LightDataSuite: Data Engine Tutorial

Welcome to the **LightDataSuite** developer guide. This suite provides two specialized engines—**GraphQLEngine** and **KqlEngine**—designed to bridge the gap between modern frontend development and traditional SQL databases.

---

## 💡 Why This Matters for Frontend Developers

Modern web applications require flexible data fetching. Standard REST APIs often suffer from two main issues:
1. **Over-fetching:** Receiving 50 fields when you only need `Name` and `ID`.
2. **Under-fetching:** Making 5 separate API calls to get a user, their orders, and their addresses.

### The Solution:
* **GraphQL:** Solves these issues by allowing you to fetch a "tree" of nested data (e.g., a Customer and all their Orders) in a **single request**.
* **KQL (Kusto Query Language):** Uses a "Pipeline" approach (`|`). It is incredibly intuitive for building dashboards or logs, as you can transform and filter data step-by-step.

---

## 1. GraphQLEngine
The `GraphQLEngine` translates nested GraphQL queries into efficient SQL `FOR JSON` statements.

### Sample Request
**POST Body:**
```json
{
  "query": "{ customers(city: $city) { name, orders(on: 'customerid', parentkey: 'id') { total, date } } }",
  "city": "london"
}
```

### Key Features
* **Sub-queries:** Use nested brackets to fetch related data automatically.
* **Arguments:** * `first`: Limits results (e.g., `customers(first: 5)`).
  * `order` / `desc`: Handles sorting.
  * `on` / `parentkey`: Defines the SQL Join logic between the table and its sub-table.
* **Variables:** Use `$` (like `$city`) to safely inject values from the JSON root, preventing SQL injection.

---

## 2. KqlEngine
The `KqlEngine` provides a linear, readable way to filter and project data using the pipe operator.

### Sample Request
**POST Body:**
```json
{
  "query": "products | where category == 'electronics' | project name, price, discounted = (price * 0.9) | order by price desc | take 5"
}
```

### Key Features
* **The Pipe (`|`):** Each segment modifies the result of the previous one.
* **`project`**: Choose specific columns. You can rename them using `NewName = OldName`.
* **`where`**: Supports standard comparisons (translated to SQL `WHERE`).
* **`take`**: Quickly limits the result set (SQL `TOP`).

---

## ⚖️ LightDataSuite vs. Standard Versions

These engines are **Lightweight Transpilers**. They give you modern syntax without the overhead of a full GraphQL server or an Azure Data Explorer cluster.

| Feature | LightDataSuite Version | Standard GraphQL/KQL |
| :--- | :--- | :--- |
| **Backend** | Translates directly to **T-SQL**. | Uses Resolvers or Kusto Engine. |
| **Case Sensitivity** | **Case-Insensitive**. Forced to `.lower()`. | Strictly Case-Sensitive. |
| **Relationships** | Manual (`on`, `parentkey`). | Automatic (based on Schema). |
| **Security** | Strict **Table/Field Whitelisting**. | RBAC / Complex Policies. |
| **Logic** | Lightweight & Flat. | Supports complex unions/joins. |

> [!IMPORTANT]
> **Why the difference?**
> Standard versions require heavy infrastructure. These engines are **Zero-Config**. They allow you to get the architectural benefits of these languages while running directly on your existing SQL Server.

---

## 🛡️ Security & Best Practices

* **No Hardcoding:** Never put sensitive values directly in the query string. Use the JSON root to pass parameters; the engine automatically creates `SqlParameter` objects.
* **Whitelisting:** The engines use `CheckTable` and `CheckFields` methods. Ensure your allowed tables and columns are configured in your `BaseDataEngine` setup.
* **Read-Only:** These engines are strictly for `SELECT` operations. They cannot perform `INSERT`, `UPDATE`, or `DELETE`.
