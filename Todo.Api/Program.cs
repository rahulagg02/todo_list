using Todo.Api.Data;
using Todo.Api.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(opts =>
  opts.AddDefaultPolicy(policy =>
    policy
      .WithOrigins("http://localhost:5173")
      .AllowAnyHeader()
      .AllowAnyMethod()
  )
);

// EF Core DbContext (SQLite)
builder.Services.AddDbContext<TodoDbContext>(opts =>
  opts.UseSqlite("Data Source=todos.db")
);

// Register both providers
builder.Services.AddSingleton<InMemoryTodoProvider>();
builder.Services.AddScoped<EfCoreTodoProvider>();

// Factory delegate: pick provider by key
builder.Services.AddScoped<Func<string, ITodoProvider>>(sp => key =>
  key == "EfCore"
    ? sp.GetRequiredService<EfCoreTodoProvider>()
    : sp.GetRequiredService<InMemoryTodoProvider>()
);

builder.Services.AddControllers();

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();

