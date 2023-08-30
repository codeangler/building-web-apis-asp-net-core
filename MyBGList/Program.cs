using Microsoft.AspNetCore.Cors;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseSqlServer(
       builder.Configuration.GetConnectionString("DefaultConnection"))
   );
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts => opts.ResolveConflictingActions(apiDesc => apiDesc.First()));
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(cfg => {
        cfg.WithOrigins(builder.Configuration["AllowedOrigins"]);
        cfg.AllowAnyHeader();
        cfg.AllowAnyMethod();
    });
    options.AddPolicy(name: "AnyOrigin",
        cfg => {
            cfg.AllowAnyOrigin();
            cfg.AllowAnyHeader();
            cfg.AllowAnyMethod();
        });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage")) app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers().RequireCors("AnyOrigin");

app.Run();

