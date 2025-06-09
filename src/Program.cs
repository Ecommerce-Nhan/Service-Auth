using IdentityService.Extentions;

var builder = WebApplication.CreateBuilder(args);
var app = builder
    .ConfigureServices()
    .ConfigurePipeline(builder);

app.Run();