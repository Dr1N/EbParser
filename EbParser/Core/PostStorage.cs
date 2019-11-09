using EbParser.Context;
using EbParser.DTO;
using EbParser.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class PostStorage : IPostStorage, IDisposable
    {
        #region Constants

        private const string Site = "https://ebanoe.it";
        private const string FileDirectory = "files";
        private const int Attemps = 4;

        #endregion

        #region Fields

        private SiteContext _db;
        private ILoader _loader; // TODO: remove

        #endregion

        #region Life

        public PostStorage()
        {
            _loader = new PageLoader();
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _db?.Dispose();
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

        public async Task SavePostAsync(string url, PostDto postDto)
        {
            using (_db = new SiteContext())
            {
                await SaveTagsAsync(postDto.Tags);
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
                SavePostTags(postDto, post);
                _db.Posts.Add(post);
                await _db.SaveChangesAsync();
                if (postDto.Comments.Count > 0)
                {
                    await SaveCommentsAsync(postDto.Comments, post);
                    await _db.SaveChangesAsync();
                }
                if (postDto.Files.Count > 0)
                {
                    await SavePostFilesAsync(postDto.Files);
                    await _db.SaveChangesAsync();
                }
            }
            _db = null;
        }

        public string GetLastPostUrl()
        {
            using (_db = new SiteContext())
            {
                var lastPost = _db.Posts
               .AsNoTracking()
               .ToList()
               .OrderBy(p => p.Publish.ToUnixTimeSeconds())
               .LastOrDefault();

                return lastPost?.Url;
            }
        }

        public bool IsExists(string url)
        {
            using (_db = new SiteContext())
            {
                return _db.Posts.Any(p => p.Url == url);
            }
        }

        #endregion

        #region Private

        private async Task SaveTagsAsync(IList<string> tags)
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
        }

        private void SavePostTags(PostDto postDto, Post post)
        {
            foreach (var tag in postDto.Tags)
            {
                var dbTag = _db.Tags.FirstOrDefault(t => t.Name == tag);
                var postTag = new PostTag() { Post = post, Tag = dbTag };
                _db.PostTags.Add(postTag);
            }
        }

        private async Task SaveCommentsAsync(IList<CommentDto> comments, Post post)
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
        }

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

        private async Task SavePostFilesAsync(IList<string> files)
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

        // TODO: Duplicate
        private async Task<bool> LoadFileAsync(string url, string newName)
        {
            var result = false;
            for (int i = 0; i < Attemps; i++)
            {
                try
                {
                    result = await _loader.LoadFileAsync(url, newName);
                }
                catch (HttpRequestException ex)
                {
                    if (ex.Message.Contains(HttpStatusCode.TooManyRequests.ToString()))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5 + i));      // Wait and continue
                    }
                    else if (ex.Message.Contains(HttpStatusCode.BadRequest.ToString()))     // Create new loader
                    {
                        (_loader as IDisposable)?.Dispose();
                        _loader = new PageLoader();
                        await Task.Delay(TimeSpan.FromSeconds(5 + i));
                    }
                }
                catch { }
            }

            return result;
        }

        // TODO: Move
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