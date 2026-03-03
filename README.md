LightDataSuite
LightDataSuite provides a lightweight data engine framework for executing queries against SQL databases using GraphQL-like and KQL-like syntax. It includes a shared base engine for safety checks and SQL execution, plus specialized engines for parsing and translating queries.

Contents
BaseDataEngine.cs  
Abstract base class that manages:

Database connection handling

Table and field validation (blocking unauthorized access)

Safe SQL execution with parameter binding

GraphQLEngine.cs  
Implements IGraphQLEngine for handling GraphQL-style queries.

Parses GraphQL AST

Translates selections and arguments into SQL queries

Supports filtering, ordering, and nested selections

KqlEngine.cs  
Implements IKqlEngine for handling KQL-style queries.

Parses pipeline segments (where, take, project, order by)

Converts them into SQL statements

Supports nested projections and aliasing

Usage
Initialize an engine with:

csharp
var gqlEngine = new GraphQLEngine(connectionString, allowedTables, blockedFields);
string result = await gqlEngine.Run(jsonElement);

var kqlEngine = new KqlEngine(connectionString, allowedTables, blockedFields);
string result = await kqlEngine.Run(jsonElement);
Results are returned as JSON (FOR JSON PATH).

Key Features
Lowercased normalization for consistency
Parameterized SQL for safety
Configurable table and field restrictions
JSON output for easy integration
