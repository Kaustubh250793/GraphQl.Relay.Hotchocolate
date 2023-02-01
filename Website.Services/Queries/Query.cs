using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Website.Data.Contexts;
using Website.Data.Entities;
using Website.Services.Data_Loaders;

namespace Website.Services.Queries
{
    /// <summary>
    /// Queries for posts and tags.
    /// </summary>
    public class Query
    {
        /// <summary>
        /// Gets the posts.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of posts.</returns>
        [UseDbContext(typeof(ApiContext))]
        [UsePaging]
        public async Task<List<Post>> GetPosts(ApiContext context,
                                               CancellationToken cancellationToken) => await context.Posts.AsNoTracking()
                                                                                                          .ToListAsync(cancellationToken);

        /// <summary>
        /// Gets the post asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dataLoader">The data loader.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The post, with the given ID.</returns>
        [NodeResolver]
        public async Task<Post?> GetPostByIdAsync(int id,
                                                  PostByIdDataLoader dataLoader,
                                                  CancellationToken cancellationToken) => await dataLoader.LoadAsync(id, cancellationToken);

        /// <summary>
        /// Gets the post by title asynchronous.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="context">The context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The post, with the given name.</returns>
        [UseDbContext(typeof(ApiContext))]
        public async Task<Post?> GetPostByTitleAsync(string title,
                                                     ApiContext context,
                                                     CancellationToken cancellationToken) => await context.Posts.AsNoTracking()
                                                                                                                .FirstOrDefaultAsync(p => p.Title == title, cancellationToken);

        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>List of tags.</returns>
        [UseDbContext(typeof(ApiContext))]
        [UsePaging]
        public async Task<List<Tag>> GetTags(ApiContext context) => await context.Tags.AsNoTracking().Include(x => x.Posts).ToListAsync();
    }
}
