using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Todo.Api.Models;

namespace Todo.Api.Providers
{
    public interface ITodoProvider
    {
        Task<IEnumerable<TodoItem>> GetAll(string search = null);
        Task<TodoItem>             Add(TodoItem todo);
        Task                       Update(TodoItem todo);
        Task                       Delete(int id);
    }

    public class InMemoryTodoProvider : ITodoProvider
    {
        private readonly List<TodoItem> _store = new();
        private int _nextId = 1;

        public Task<IEnumerable<TodoItem>> GetAll(string search = null)
        {
            var query = _store.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(query);
        }

        public Task<TodoItem> Add(TodoItem todo)
        {
            todo.Id = _nextId++;
            _store.Add(todo);
            return Task.FromResult(todo);
        }

        public Task Update(TodoItem todo)
        {
            var idx = _store.FindIndex(t => t.Id == todo.Id);
            if (idx >= 0) _store[idx] = todo;
            return Task.CompletedTask;
        }

        public Task Delete(int id)
        {
            var item = _store.FirstOrDefault(t => t.Id == id);
            if (item != null) _store.Remove(item);
            return Task.CompletedTask;
        }
    }
}
