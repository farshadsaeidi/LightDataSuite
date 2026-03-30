using LightDataSuite;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var config = new EngineConfig
{
    ConnectionString = "Data Source=.;Initial Catalog=Emerald5;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;",
    CaseSensitive = false,
    AllowedTables = "MasterAccounts|subaccounts|Invoices",
    BlockedFields = "Password",
    MaxNestedLevel = 3,
    MaxRows = 100,
    MaxFields = 30,
    MaxParameters = 10,
    MaxJsonLength = 262144,
    MaxQueryLength = 2048,
    CommandTimeout = 15
};

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<IKqlEngine, KqlEngine>();
builder.Services.AddSingleton<IGraphQLEngine, GraphQLEngine>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost") 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Keep your existing:
var app = builder.Build();
app.UseCors("Development");  // Before endpoints!

app.UseHttpsRedirection();

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
KQL
{
  "query": "MasterAccounts | where CustomerID == @id | top 2 by CustomerID | project FirstName, MiddleName, LastName, CustomerID, Invoices = ( Invoices | where CustomerID == MasterAccounts.CustomerID | order by InvoiceID desc | project InvoiceID, Amount, CreateDate | take 2  )",
  "id": 10540
}

GQL
{
  "query":"{ masteraccounts(customerid: $id, first: 2) { firstname middlename lastname customerid Invoices(on: 'customerid', parentKey: 'customerid', first: 2, order: 'invoiceid', desc: true) { invoiceid amount createdate } } }",
  "id":1230
}


*/
