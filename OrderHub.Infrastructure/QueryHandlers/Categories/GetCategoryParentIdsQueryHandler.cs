using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Queries.CategoryQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Categories
{
    internal class GetCategoryParentIdsQueryHandler(AppDbContextFactory dbContextFactory) : IRequestHandler<GetCategoryParentIdsQuery, IEnumerable<int>>
    {
        private readonly AppDbContextFactory _dbContextFactory = dbContextFactory;

        public async Task<IEnumerable<int>> Handle(GetCategoryParentIdsQuery request, CancellationToken cancellationToken)
        {
            using AppDbContext appDbContext = _dbContextFactory.CreateDbContext();
            string sql = $@"WITH RECURSIVE ParentHierarchy AS (
                    -- Start with the child
                    SELECT id, name, parent_category_id
                    FROM categories
                    WHERE Id = @child_id

                    UNION ALL

                    -- Get parent
                    SELECT c.id, c.name, c.parent_category_id
                    FROM categories c
                    INNER JOIN ParentHierarchy ph ON c.id = ph.parent_category_id
                    )
                    SELECT id
                    FROM ParentHierarchy";
            return await appDbContext.Database.SqlQueryRaw<int>(sql, new SqliteParameter("@child_id", request.CategoryId)).ToListAsync();
        }
    }
}
