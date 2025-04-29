using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Todo.Api.Models;
using Todo.Api.Providers;

namespace Todo.Api.Controllers
{
    // Handles HTTP requests for to-do
    // Chooses the provider based on the "X-Provider" header.
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController : ControllerBase
    {
        // Factory delegate injected via DI that returns an ITodoProvider
        private readonly Func<string, ITodoProvider> _providerFactory;
        
        // Constructor accepts a provider factory delegate and maps a string key to an ITodoProvider instance.
        public TodosController(Func<string, ITodoProvider> providerFactory)
            => _providerFactory = providerFactory;

        // Gets the current provider based on the request header.
        private ITodoProvider Provider
            => _providerFactory(
                Request.Headers["X-Provider"].FirstOrDefault() ?? "InMemory"
            );

        // Retrieves all to-do items, optionally filtered by a search term.
        [HttpGet]
        public Task<IEnumerable<TodoItem>> Get([FromQuery] string search = null)
            => Provider.GetAll(search);

        // Creates a new to-do item.
        [HttpPost]
        public Task<TodoItem> Post([FromBody] TodoItem t)
            => Provider.Add(t);

        // Updates an existing to-do item by Id.
        [HttpPut("{id}")]
        public Task Put(int id, [FromBody] TodoItem t)
        {
            t.Id = id;
            return Provider.Update(t);
        }

        // Deletes a to-do item by Id.
        [HttpDelete("{id}")]
        public Task Delete(int id) => Provider.Delete(id);
    }
}
