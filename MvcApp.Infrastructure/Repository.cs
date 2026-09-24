using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using System.Linq;
using System.Linq.Expressions;

namespace MvcApp.Infrastructure
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly DbContext context;
        private readonly DbSet<TEntity> entities;

        public Repository(UserDbContext userDb)
        {
            context = userDb;
            entities = context.Set<TEntity>();
        }

        public IQueryable<TEntity> Query()
        {
            return entities.AsQueryable();
        }

        public IEnumerable<TEntity> GetAll()
        {
            return entities.AsEnumerable();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await entities.ToListAsync();
        }

        public TEntity Get(Guid id)
        {

            return entities.Find(id)!;
        }

        public async Task<TEntity> GetAsync(Guid id)
        {
            var result = await entities.FindAsync(id);
            ArgumentNullException.ThrowIfNull(result);

            return result;
        }

        public void Add(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            entities.Add(entity);
            context.SaveChanges();
        }

        public async Task AddAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            entities.Add(entity);
            await context.SaveChangesAsync();
        }

        public void Update(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            entities.Update(entity);
            context.SaveChanges();
        }

        public async Task UpdateAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            entities.Update(entity);
            await context.SaveChangesAsync();
        }

        public void Delete(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            entities.Remove(entity);
            context.SaveChanges();
        }

        public async Task DeleteAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            entities.Remove(entity);
            await context.SaveChangesAsync();
        }

        public async Task<TEntity?> GetByIdAsync(int? id)
        {
            var result = await entities.FindAsync(id);
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return result;
        }

        public async Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await context.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entities.Remove(entity);
                await context.SaveChangesAsync();
            }
        }
        public async Task DeleteUserAsync(string id)
        {
            var entity = await GetFirstOrDefaultAsync(e => EF.Property<string>(e, "Id") == id);
            if (entity != null)
            {
                entities.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

    }
}
