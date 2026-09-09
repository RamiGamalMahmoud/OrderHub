using Microsoft.AspNetCore.Builder;

using OrderHub.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ApiConfiguration.ConfigureServices(builder);

WebApplication app = builder.Build();

ApiConfiguration.ConfigureApplication(app);

app.Run();
