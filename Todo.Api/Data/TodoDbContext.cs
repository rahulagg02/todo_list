using Microsoft.EntityFrameworkCore;
using Todo.Api.Models;

namespace Todo.Api.Data
{
    // EF Core DbContext for to-do items
    public class TodoDbContext : DbContext
    {
        public DbSet<TodoItem> Todos { get; set; }
        public TodoDbContext(DbContextOptions<TodoDbContext> opts) : base(opts) {}
    }
}
