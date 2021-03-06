/*
 * Copyright (c) 2019-2020 Håkan Edling
 *
 * This software may be modified and distributed under the terms
 * of the MIT license.  See the LICENSE file for details.
 *
 * http://github.com/piranhacms/piranha
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Piranha.Extend;
using Piranha.Extend.Blocks;
using Piranha.Models;

namespace Piranha.Azure.Search.Services
{
    /// <summary>
    /// The identity module.
    /// </summary>
    public class ContentSearchService
    {
        private readonly string _serviceName = "";
        private readonly string _apiKey = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="serviceName">The search service name</param>
        /// <param name="apiKey">The admin api key</param>
        public ContentSearchService(string serviceName, string apiKey)
        {
            _serviceName = serviceName;
            _apiKey = apiKey;
        }

        /// <summary>
        /// Creates the main search indexes.
        /// </summary>
        public void CreateIndexes()
        {
            using (var client = CreateClient())
            {
                var contentIndex = new Index()
                {
                    Name = "content",
                    Fields = FieldBuilder.BuildForType<Content>()
                };
                client.Indexes.CreateOrUpdate(contentIndex);
            }
        }

        /// <summary>
        /// Creates or updates the searchable content for the
        /// given page.
        /// </summary>
        /// <param name="page">The page</param>
        public Task PageSaveAsync(PageBase page)
        {
            using (var client = CreateClient())
            {
                var indexClient = client.Indexes.GetClient("content");
                var body = new StringBuilder();

                foreach (var block in page.Blocks)
                {
                    if (block is ISearchable searchableBlock)
                    {
                        body.AppendLine(searchableBlock.GetIndexedContent());
                    }
                }

                var cleanHtml = new Regex("<[^>]*(>|$)");
                var cleanSpaces = new Regex("[\\s\\r\\n]+");

                var cleaned = cleanSpaces.Replace(cleanHtml.Replace(body.ToString(), " "), " ").Trim();

                var actions = new IndexAction<Content>[]
                {
                    IndexAction.MergeOrUpload(
                        new Content
                        {
                            Slug = page.Slug,
                            ContentId = page.Id.ToString(),
                            ContentType = "page",
                            Title = page.Title,
                            Body = cleaned
                        }
                    )
                };
                var batch = IndexBatch.New(actions);

                return indexClient.Documents.IndexAsync(batch);
            }
        }

        /// <summary>
        /// Deletes the given page from the search index.
        /// </summary>
        /// <param name="page">The page to delete</param>
        public Task PageDeleteAsync(PageBase page)
        {
            using (var client = CreateClient())
            {
                var indexClient = client.Indexes.GetClient("content");

                var batch = IndexBatch.Delete("content", new List<string> { page.Id.ToString() });

                return indexClient.Documents.IndexAsync(batch);
            }
        }

        /// <summary>
        /// Creates or updates the searchable content for the
        /// given post.
        /// </summary>
        /// <param name="post">The post</param>
        public Task PostSaveAsync(PostBase post)
        {
            using (var client = CreateClient())
            {
                var indexClient = client.Indexes.GetClient("content");
                var body = new StringBuilder();

                foreach (var block in post.Blocks)
                {
                    if (block is ISearchable searchableBlock)
                    {
                        body.AppendLine(searchableBlock.GetIndexedContent());
                    }
                }

                var cleanHtml = new Regex("<[^>]*(>|$)");
                var cleanSpaces = new Regex("[\\s\\r\\n]+");

                var cleaned = cleanSpaces.Replace(cleanHtml.Replace(body.ToString(), " "), " ").Trim();

                var actions = new IndexAction<Content>[]
                {
                    IndexAction.MergeOrUpload(
                        new Content
                        {
                            Slug = post.Slug,
                            ContentId = post.Id.ToString(),
                            ContentType = "post",
                            Title = post.Title,
                            Category = post.Category.Title,
                            Tags = post.Tags.Select(t => t.Title).ToList(),
                            Body = cleaned
                        }
                    )
                };
                var batch = IndexBatch.New(actions);

                return indexClient.Documents.IndexAsync(batch);
            }
        }

        /// <summary>
        /// Deletes the given post from the search index.
        /// </summary>
        /// <param name="post">The post to delete</param>
        public Task PostDeleteAsync(PostBase post)
        {
            using (var client = CreateClient())
            {
                var indexClient = client.Indexes.GetClient("content");

                var batch = IndexBatch.Delete("content", new List<string> { post.Id.ToString() });

                return indexClient.Documents.IndexAsync(batch);
            }
        }

        /// <summary>
        /// Creates the search client.
        /// </summary>
        private SearchServiceClient CreateClient()
        {
            return new SearchServiceClient(_serviceName, new SearchCredentials(_apiKey));
        }
    }
}