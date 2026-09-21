namespace OrderHub.Infrastructure.Reopsitories;
internal abstract class RepositoryBase<TModel> where TModel : class
{
    protected readonly AppDbContext _appDbContext;

    public RepositoryBase(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public abstract void Add(TModel model);
}
