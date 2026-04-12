using ToDoList.Core.Interfaces;
using ToDoList.Data;
using ToDoList.Data.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// Repositories
builder.Services.AddScoped<TodoListRepository>();
builder.Services.AddScoped<TodoItemRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<DeletedEntityRepository>();

// Controllers
builder.Services.AddControllers();

// CORS (allow all clients during development)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

WebApplication app = builder.Build();

app.UseCors();

// Serve documentation site from wwwroot/docs
app.UseStaticFiles();

app.MapControllers();

app.Run();
