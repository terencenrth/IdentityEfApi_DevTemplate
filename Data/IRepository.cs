using System.Linq.Expressions;

namespace IdentityEfApi.Data
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
        Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
        Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<T> AddAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(T entity, CancellationToken ct = default);
        IQueryable<T> Query(); // For advanced read scenarios
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
