using EbParser.Context;
using EbParser.DTO;
using EbParser.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class PostStorage : IPostStorage, IDisposable
    {
        #region Constants

        private const string Site = "https://ebanoe.it";
        private const string FileDirectory = "files";

        #endregion

        #region Fields

        private readonly ILoader _loader;
        private readonly SiteContext _db;

        #endregion

        #region Life

        public PostStorage(ILoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _db = new SiteContext();
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _db.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #endregion

        #region IPostStorage Implementation

        public async Task<IList<Tag>> SaveTagsAsync(IList<string> tags)
        {
            foreach (var tagName in tags)
            {
                var dbTag = _db.Tags.FirstOrDefault(t => t.Name == tagName);
                if (dbTag == null)
                {
                    _db.Tags.Add(new Tag() { Name = tagName });
                }
            }
            await _db.SaveChangesAsync();

            return _db.Tags.ToList();
        }

        public async Task<Post> SavePostAsync(PostDto postDto, string url)
        {
            var post = new Post()
            {
                Url = url,
                Title = postDto.Title,
                Publish = postDto.Publish,
                Poster = postDto.Poster,
                Content = postDto.Content,
                Category = postDto.Category,
                Updated = DateTime.Now,
            };
            foreach (var tag in postDto.Tags)
            {
                var dbTag = _db.Tags.FirstOrDefault(t => t.Name == tag);
                var postTag = new PostTag() { Post = post, Tag = dbTag };
                _db.PostTags.Add(postTag);
            }
            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            return post;
        }

        public async Task<IList<Comment>> SaveCommentsAsync(IList<CommentDto> comments, Post post)
        {
            var firstLevelComments = comments
                .Where(c => c.ParrentId == 0)
                .ToList();
            foreach (var comment in firstLevelComments)
            {
                var dbComment = new Comment()
                {
                    Author = comment.Author,
                    Publish = comment.Publish,
                    Post = post,
                    Content = comment.Content,
                    Parent = null,
                    Updated = DateTime.Now,
                };
                _db.Comments.Add(dbComment);
                await SaveChildCommentsAsync(comments, comment, dbComment, post);
            }
            await _db.SaveChangesAsync();

            return _db.Comments.ToList();
        }

        public async Task<IList<Context.File>> SavePostFilesAsync(IList<string> files)
        {
            var tasks = new List<Task>();
            var bag = new ConcurrentBag<Context.File>();
            foreach (var fileSrc in files)
            {
                var task = Task.Run(async () =>
                {
                    var name = await SaveSiteFileAsync(fileSrc);
                    if (!string.IsNullOrEmpty(name))
                    {
                        var file = new Context.File()
                        {
                            Url = fileSrc,
                            FileName = name,
                        };
                        bag.Add(file);
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            _db.Files.AddRange(bag);
            await _db.SaveChangesAsync();

            return _db.Files.ToList();
        }

        public string GetLastPostUrl()
        {
            var result = string.Empty;
            var lastPost = _db.Posts
                .ToList()
                .OrderBy(p => p.Publish.ToUnixTimeSeconds())
                .LastOrDefault();
            
            return lastPost?.Url;
        }

        public bool IsExists(string url)
        {
            return _db.Posts.Any(p => p.Url == url);
        }

        #endregion

        #region Private

        private async Task SaveChildCommentsAsync(IList<CommentDto> allComments, 
            CommentDto parent, 
            Comment parentModel, 
            Post post)
        {
            var children = allComments
                .Where(c => c.ParrentId == parent.Id)
                .ToList();
            if (children.Count == 0)
            {
                return;
            }

            foreach (var child in children)
            {
                var dbComment = new Comment()
                {
                    Author = child.Author,
                    Publish = child.Publish,
                    Post = post,
                    Content = child.Content,
                    Parent = parentModel,
                    Updated = DateTime.Now,
                };
                _db.Comments.Add(dbComment);
                await SaveChildCommentsAsync(allComments, child, dbComment, post);
            }
        }

        private async Task<string> SaveSiteFileAsync(string url)
        {
            string result = null;

            if (!url.StartsWith(Site))
            {
                return result;
            }
            var fileName = url.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(fileName))
            {
                return result;
            }

            var directory = CreateFilesDirecory();
            var ext = Path.GetExtension(fileName);
            var newName = Path.Combine(directory, Guid.NewGuid().ToString() + ext);
            await LoadFileAsync(url, newName);
            result = newName;

            return result;
        }

        private async Task<bool> LoadFileAsync(string url, string newName)
        {
            var result = false;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    result = await _loader.LoadFileAsync(url, newName);
                }
                catch (HttpRequestException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5 + i));
                }
            }

            return result;
        }

        private string CreateFilesDirecory()
        {
            if (!Directory.Exists(FileDirectory))
            {
                Directory.CreateDirectory(FileDirectory);
            }
            var dirInfo = new DirectoryInfo(FileDirectory);

            return dirInfo.FullName;
        }

        #endregion
    }
}