using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public interface ICategoryReader
{
    Task<GetCategoriesResponse> GetCategoriesAsync(int? parentId, CancellationToken cancellationToken);
    Task<CategoryEdit> GetCategoryForEditAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<CategoryInfo>> GetCategoriesInfoAsync(CancellationToken cancellationToken);
}
