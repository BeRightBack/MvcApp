using System.Linq;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services
{
    public class GenericService<TEntity>(IRepository<TEntity> repository) : IGenericService<TEntity> where TEntity : class
    {
        private readonly IRepository<TEntity> _repository = repository;

        public IQueryable<TEntity> Query()
        {
            return _repository.Query();
        }

        public IEnumerable<TEntity> GetAll()
        {
            return _repository.GetAll();
        }
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }
        public TEntity Get(Guid id)
        {
            return _repository.Get(id);
        }

        public async Task<TEntity> GetAsync(Guid id)
        {
            return await _repository.GetAsync(id);
        }
        public void Add(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _repository.Add(entity);
        }

        public async Task AddAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await _repository.AddAsync(entity);
        }
        public void Update(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _repository.Update(entity);
        }
        public async Task UpdateAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await _repository.UpdateAsync(entity);
        }
        public void Delete(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _repository.Delete(entity);
        }

        public async Task DeleteAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await _repository.DeleteAsync(entity);
        }
        public void Delete(IList<Guid> ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            foreach (var id in ids)
            {
                TEntity entity = _repository.Get(id);
                _repository.Delete(entity);
            }
            ;
        }

        public async Task DeleteAsync(IList<Guid> ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            foreach (var id in ids)
            {
                TEntity entity = await _repository.GetAsync(id);
                await _repository.DeleteAsync(entity);
            }
            ;
        }
    }

    public interface IGenericService<TEntity> where TEntity : class
    {
        IQueryable<TEntity> Query();
        IEnumerable<TEntity> GetAll();
        Task<IEnumerable<TEntity>> GetAllAsync();
        TEntity Get(Guid id);
        Task<TEntity> GetAsync(Guid id);
        void Add(TEntity entity);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        Task UpdateAsync(TEntity entity);
        void Delete(TEntity entity);
        Task DeleteAsync(TEntity entity);
        void Delete(IList<Guid> ids);
        Task DeleteAsync(IList<Guid> ids);
    }
}