using LightDataSuite;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var config = new EngineConfig
{
    ConnectionString = "Data Source=.;Initial Catalog=MyDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;",
    AllowedTables = "MasterAccounts|subaccounts|Invoices",
    BlockedFields = "Password|OTP|Pin",

    //------------------------------------------------------------------
    // Uncomment and adjust the following settings if you need
    //------------------------------------------------------------------
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
