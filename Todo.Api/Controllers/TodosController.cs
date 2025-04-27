using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Todo.Api.Models;
using Todo.Api.Providers;

namespace Todo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController : ControllerBase
    {
        private readonly Func<string, ITodoProvider> _providerFactory;
        public TodosController(Func<string, ITodoProvider> providerFactory)
            => _providerFactory = providerFactory;

        private ITodoProvider Provider
            => _providerFactory(
                Request.Headers["X-Provider"].FirstOrDefault() ?? "InMemory"
            );

        [HttpGet]
        public Task<IEnumerable<TodoItem>> Get([FromQuery] string search = null)
            => Provider.GetAll(search);

        [HttpPost]
        public Task<TodoItem> Post([FromBody] TodoItem t)
            => Provider.Add(t);

        [HttpPut("{id}")]
        public Task Put(int id, [FromBody] TodoItem t)
        {
            t.Id = id;
            return Provider.Update(t);
        }

        [HttpDelete("{id}")]
        public Task Delete(int id) => Provider.Delete(id);
    }
}
