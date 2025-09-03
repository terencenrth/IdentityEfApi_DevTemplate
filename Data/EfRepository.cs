using IdentityEfApi.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IdentityEfApi.Data
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _db;
        private readonly DbSet<T> _set;

        public EfRepository(AppDbContext db)
        {
            _db = db;
            _set = db.Set<T>();
        }

        public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
            => await _set.FindAsync(new[] { id }, ct);

        public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
            => await _set.AsNoTracking().ToListAsync(ct);

        public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);

        public async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _set.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _set.Update(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(T entity, CancellationToken ct = default)
        {
            _set.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }

        public IQueryable<T> Query() => _set.AsNoTracking();

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
