using BlogSite.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDevCorsPolicy();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(BlogApiServiceExtensions.DevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
