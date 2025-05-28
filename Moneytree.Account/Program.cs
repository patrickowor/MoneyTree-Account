using Moneytree.Accounts.Src.Internal.Filters;

using Newtonsoft.Json.Serialization;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };
    });

    builder.Services.AddOpenApi(c =>
    {
        c.AddSchemaTransformer<OpenApiSnakeCaseSchemaFilter>();
    });
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Moneytree Accounts API");
    });
}
{
    app.UseCors();
    app.UseHttpsRedirection();
    app.MapControllers();
}

app.Run();

