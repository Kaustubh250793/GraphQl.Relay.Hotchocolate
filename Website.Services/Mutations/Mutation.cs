using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Subscriptions;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Website.Core.Errors;
using Website.Data.Contexts;
using Website.Data.Entities;
using Website.Services.Inputs;
using Website.Services.Payloads;
using Website.Services.Subscriptions;

namespace Website.Services.Mutations
{
    /// <summary>
    /// Mutations for posts.
    /// </summary>
    public class Mutation
    {
        /// <summary>
        /// Adds the post asynchronous.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="context">The context.</param>
        /// <returns>Add post payload.</returns>
        [UseDbContext(typeof(ApiContext))]
        public async Task<AddPostPayload> AddPostAsync(AddPostInput input, ApiContext context)
        {
            Post post = new()
            {
                Title = input.Title,
                Content = input.Content,
                Created = DateTimeOffset.UtcNow
            };

            if (await context.Posts.FirstOrDefaultAsync(p => p.Title == input.Title) != null)
            {
                return new AddPostPayload(new ApiError("POST_WITH_TITLE_EXISTS", "A post with that title already exists."));
            }

            post = context.Posts.Add(post).Entity;

            await context.SaveChangesAsync();

            var tagIds = input.Tags.ConvertAll(t => t.Id);

            post.Tags = await context.Tags.AsNoTracking().Where(t => tagIds.Contains(t.Id)).ToListAsync();

            await context.SaveChangesAsync();

            return new AddPostPayload(post);
        }

        /// <summary>
        /// Updates the post asynchronous.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="eventSender">The event sender.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="context">The context.</param>
        /// <returns>Update post payload.</returns>
        [UseDbContext(typeof(ApiContext))]
        public async Task<UpdatePostPayload> UpdatePostAsync(UpdatePostInput input,
                                                             [Service] ITopicEventSender eventSender,
                                                             CancellationToken cancellationToken,
                                                             ApiContext context)
        {
            var post = await context.Posts.Include(p => p.Tags)
                                           .SingleOrDefaultAsync(p => p.Id == input.Id);

            if (post == null)
            {
                return new UpdatePostPayload(new ApiError("POST_NOT_FOUND", "Post not found."));
            }

            var postWithTitle = await context.Posts.FirstOrDefaultAsync(p => p.Title == input.Title);

            if (postWithTitle != null && postWithTitle.Id != post.Id)
            {
                return new UpdatePostPayload(new ApiError("POST_WITH_TITLE_EXISTS", "A post with that title already exists."));
            }

            await ApplyUpdatedValuesToPost(post, input, context);

            await context.SaveChangesAsync();

            await eventSender.SendAsync(nameof(Subscription.OnPostUpdatedAsync), post.Id, cancellationToken);

            return new UpdatePostPayload(post);
        }

        /// <summary>
        /// Adds the tag asynchronous.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="context">The context.</param>
        /// <returns>Add tag payload.</returns>
        [UseDbContext(typeof(ApiContext))]
        public async Task<AddTagPayload> AddTagAsync(AddTagInput input, ApiContext context)
        {
            Tag tag = new()
            {
                Name = input.Name
            };

            tag = context.Tags.Add(tag).Entity;

            await context.SaveChangesAsync();

            return new AddTagPayload(tag);
        }

        /// <summary>
        /// Deletes the tag asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="context">The context.</param>
        /// <returns>Delete tag payload.</returns>
        [NodeResolver]
        [UseDbContext(typeof(ApiContext))]
        public async Task<DeleteTagPayload> DeleteTagAsync(int id, ApiContext context)
        {
            var tag = await context.Tags.FindAsync(id);

            if (tag == null)
            {
                return new DeleteTagPayload(new ApiError("TAG_NOT_FOUND", "Tag not found."));
            }

            context.Tags.Remove(tag);

            await context.SaveChangesAsync();

            return new DeleteTagPayload(tag);
        }

        #region Helper methods
        /// <summary>
        /// Applies the updated values to post.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <param name="input">The input.</param>
        /// <param name="context">The context.</param>
        private static async Task ApplyUpdatedValuesToPost(Post post, UpdatePostInput input, ApiContext context)
        {
            post.Title = string.IsNullOrEmpty(input.Title) ? post.Title : input.Title;
            post.Content = string.IsNullOrEmpty(input.Content) ? post.Content : input.Content;
            post.Modified = DateTimeOffset.UtcNow;

            if (input.Tags is null)
            {
                return;
            }

            var updatedTagIds = input.Tags.ConvertAll(t => t.Id);

            post.Tags.RemoveAll(t => !updatedTagIds.Contains(t.Id));

            var currentTagIds = post.Tags.ConvertAll(t => t.Id);

            var tagsToAdd = await context.Tags.AsNoTracking()
                                                    .Where(t => updatedTagIds.Contains(t.Id) && !currentTagIds.Contains(t.Id))
                                                    .ToListAsync();

            post.Tags.AddRange(tagsToAdd);
        }
        #endregion
    }
}
