using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Application.Features.Setup.Categories;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public interface ICategoryReader
{
    Task<CategoryEdit> GetCategoryForEditAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<CategoryListItem>> GetCategoryListAsync(int? parentId, CancellationToken cancellationToken);
    Task<IEnumerable<int>> GetCategoryParentIdsAsync(int categoryId, CancellationToken cancellationToken);
    Task<IEnumerable<CategoryTree>> GetCategoryTreeAsync(CancellationToken cancellationToken);
    Task<IEnumerable<CategoryInfo>> GetCategoriesInfoAsync(CancellationToken cancellationToken);
    Task<IEnumerable<CategoryInfo>> GetRootCategoriesAsync(CancellationToken cancellationToken);
    Task<IEnumerable<CategoryInfo>> GetSubCategoriesAsync(int parentCategoryId, CancellationToken cancellationToken);
}
