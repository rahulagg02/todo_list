using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Data;
using Todo.Api.Models;

namespace Todo.Api.Providers
{
    // EF Core implementation of ITodoProvider using SQLite
    public class EfCoreTodoProvider : ITodoProvider
    {
        private readonly TodoDbContext _db;
        public EfCoreTodoProvider(TodoDbContext db) => _db = db;

        public async Task<IEnumerable<TodoItem>> GetAll(string search = null)
        {
            var q = _db.Todos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                //q = q.Where(t => t.Title.Contains(search));
                q = q.Where(t => EF.Functions.Like(t.Title, $"%{search}%"));
            return await q.ToListAsync();
        }


        public async Task<TodoItem> Add(TodoItem todo)
        {
            var e = (await _db.Todos.AddAsync(todo)).Entity;
            await _db.SaveChangesAsync();
            return e;
        }

        public async Task Update(TodoItem todo)
        {
            _db.Todos.Update(todo);
            await _db.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var e = await _db.Todos.FindAsync(id);
            if (e != null) { _db.Todos.Remove(e); await _db.SaveChangesAsync(); }
        }
    }
}
