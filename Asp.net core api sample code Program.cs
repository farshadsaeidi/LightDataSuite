using LightDataSuite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string connectionString = "...";
string tables = "MasterAccounts|subaccounts|Invoices";
string blockFields = "Password|OTPinint";

builder.Services.AddSingleton<IKqlEngine>(new KqlEngine(connectionString, tables, blockFields));
builder.Services.AddSingleton<IGraphQLEngine>(new GraphQLEngine(connectionString, tables, blockFields));

var app = builder.Build();

app.MapPost("/KQL", async (JsonElement body, IKqlEngine kql) =>
{
        var jsonResult = await kql.Run(body);
        return Results.Content(jsonResult, "application/json");
});


app.MapPost("/GQL", async (JsonElement body, IGraphQLEngine gql) =>
{
        var jsonResult = await gql.Run(body);
        return Results.Content(jsonResult, "application/json");
});

app.Run();


/*
{
"query":"{ masteraccounts(customerid: 1230, first: 2) { firstname middlename lastname customerid Invoices(on: 'customerid', parentKey: 'customerid', first: 2, order: 'invoiceid', desc: true) { invoiceid amount createdate } } }"
}

{
  "query":"{ masteraccounts(customerid: $id, first: 2) { firstname middlename lastname customerid Invoices(on: 'customerid', parentKey: 'customerid', first: 2, order: 'invoiceid', desc: true) { invoiceid amount createdate } } }",
  "id":1230
}

{
  "query": "MasterAccounts | where CustomerID == 1230 | top 2 by CustomerID | project FirstName, MiddleName, LastName, CustomerID, Invoices = ( Invoices | where CustomerID == MasterAccounts.CustomerID | order by InvoiceID desc | project InvoiceID, Amount, CreateDate | take 2  )"
}

{
  "query": "MasterAccounts | where CustomerID == @id | top 2 by CustomerID | project FirstName, MiddleName, LastName, CustomerID, Invoices = ( Invoices | where CustomerID == MasterAccounts.CustomerID | order by InvoiceID desc | project InvoiceID, Amount, CreateDate | take 2  )",
  "id":1230
}


*/
