using IdentityService.Extentions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomDbContext(builder);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddCustomOpenIddict();
builder.Services.AddHttpClient();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDeveloperExceptionPage();
app.UseCors();
app.MapControllers();

app.Run();

