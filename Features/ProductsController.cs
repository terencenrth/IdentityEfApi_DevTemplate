using IdentityEfApi.Data;
using IdentityEfApi.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityEfApi.Features
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // require JWT
    public class ProductsController : ControllerBase
    {
        private readonly IRepository<Product> _repo;

        public ProductsController(IRepository<Product> repo) => _repo = repo;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> Get(CancellationToken ct)
            => Ok(await _repo.ListAsync(ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Product>> GetById(int id, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(id, ct);
            return entity is null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create(Product model, CancellationToken ct)
        {
            var created = await _repo.AddAsync(model, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Product model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();
            await _repo.UpdateAsync(model, ct);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var existing = await _repo.GetByIdAsync(id, ct);
            if (existing is null) return NotFound();
            await _repo.DeleteAsync(existing, ct);
            return NoContent();
        }
    }
}
