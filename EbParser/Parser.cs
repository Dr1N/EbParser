using EbParser.Context;
using EbParser.Core;
using EbParser.DTO;
using EbParser.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EbParser
{
    class Parser : IParser, IDisposable
    {
        #region Constants

        private const string Site = "https://ebanoe.it";
        private const string Pattern = "https://ebanoe.it/page/{0}/";
        private const string FileDirectory = "files";

        #endregion

        #region Fields

        private readonly bool _saveFiles;
        private readonly IPageLoader _loader;
        private readonly IHtmlParser _parser;
        private readonly SiteContext _db;

        #endregion

        #region Events

        public event EventHandler<Uri> PageChangded = delegate { };
        public event EventHandler<string> Error = delegate { };
        public event EventHandler<string> Report = delegate { };

        #endregion

        #region Life

        public Parser(bool saveFiles)
        {
            _db = new SiteContext();
            _loader = new PageLoader();
            _parser = new AngelParser();
            _saveFiles = saveFiles;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    (_loader as IDisposable)?.Dispose();
                    (_parser as IDisposable)?.Dispose();
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

        #region IParser Implementation

        public async Task ParseAsync()
        {
            await ParseSiteAsync();
        }

        #endregion

        #region Private

        #region Parsing

        private async Task ParseSiteAsync()
        {
            try
            {
                RaiseReport("START");
                var pages = await ParsePagesCountAsync();
                RaiseReport($"Pages: { pages }");
                var lastUrl = GetLastPostUrl();
                RaiseReport($"Last: { lastUrl }");
                var isEnd = false;
                for (int i = 0; i <= pages; i++)
                {
                    try
                    {
                        if (i == 1) continue; // Skip first page
                        var pageUrl = string.Format(Pattern, i);
                        RaiseReport(new string('-', 40));
                        RaisePage(new Uri(pageUrl));
                        RaiseReport(new string('-', 40));
                        var linkTags = await GetPostLinksFromPageAsync(pageUrl);
                        RaiseReport($"Find: { linkTags.Count } posts");
                        var stopWatch = Stopwatch.StartNew();
                        foreach (var linkTag in linkTags)
                        {
                            try
                            {
                                var postUrl = await _parser.ParseAttributeAsync(linkTag, "a", "href");
                                RaiseReport(new string('-', 40));
                                RaisePage(new Uri(postUrl));
                                RaiseReport(new string('-', 40));
                                if (postUrl == lastUrl)
                                {
                                    isEnd = true;
                                    break;
                                }
                                var html = await LoadPageAsync(postUrl);
                                stopWatch.Restart();
                                using var postParser = new PostParser(html);
                                var postDto = await postParser.GetPostDtoAsync();
                                await SaveTagsAsync(postDto.Tags);
                                var postModel = await SavePostAsync(postDto, postUrl);
                                RaiseReport($"Page saved: { stopWatch.Elapsed.TotalMilliseconds } ms");
                                stopWatch.Restart();
                                var commentDto = await postParser.GetPostCommentsAsync();
                                await SaveCommentsAsync(commentDto, postModel);
                                RaiseReport($"Comments saved: { stopWatch.Elapsed.TotalMilliseconds } ms");
                                var files = await postParser.GetPostFilesAsync();
                                stopWatch.Restart();
                                await SavePostFilesAsync(files);
                                RaiseReport($"Fies saved: { stopWatch.Elapsed.TotalMilliseconds } ms");
                            }
                            catch (Exception ex)
                            {
                                RaiseError(ex.Message);
                            }
                        }
                        break; // TODO: debug
                    }
                    catch (Exception ex)
                    {
                        RaiseError(ex.Message);
                    }
                    if (isEnd)
                    {
                        break;
                    }
                }
                RaiseReport("DONE!");
            }
            catch (Exception ex)
            {
                RaiseError(ex.Message);
            }
        }

        private async Task<int> ParsePagesCountAsync()
        {
            var mainPageHtml = await LoadPageAsync(string.Format(Pattern, 0));
            var pageLinks = await _parser.ParseHtmlAsync(mainPageHtml, EbSelectors.PagesSelector);
            var lastPage = pageLinks.Last();
            var result = await _parser.ParseTextAsync(lastPage, "a");

            return int.Parse(result);
        }
       
        private async Task<string> LoadPageAsync(string url)
        {
            var stopWatch = Stopwatch.StartNew();
            var result = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    result = await _loader.LoadPageAsync(url);
                }
                catch (HttpRequestException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5 + i));
                }
            }
            RaiseReport($"Page loaded: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            stopWatch.Stop();

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
                catch (HttpRequestException ex)
                {
                    RaiseError(ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5 + i));
                }
                catch (Exception ex)
                {
                    RaiseError(ex.Message);
                }
            }

            return result;
        }

        private async Task<IList<string>> GetPostLinksFromPageAsync(string pageUrl)
        {
            var content = await LoadPageAsync(pageUrl);
            var postTitles = await _parser.ParseHtmlAsync(content, EbSelectors.PostLinkSelector);

            return postTitles;
        }

        #endregion

        #region Saving

        private async Task<IList<Tag>> SaveTagsAsync(IList<string> tags)
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

        private async Task<Post> SavePostAsync(PostDto postDto, string url)
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

        private async Task<IList<Comment>> SaveCommentsAsync(IList<CommentDto> comments, Post post)
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

        private async Task SaveChildCommentsAsync(IList<CommentDto> allComments, CommentDto parent, Comment parentModel, Post post)
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

        private async Task<IList<Context.File>> SavePostFilesAsync(IList<string> files)
        {
            if (_saveFiles)
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

            return _db.Files.ToList();
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

        #endregion

        #region Helpers

        private string GetLastPostUrl()
        {
            var result = string.Empty;
            try
            {
                var lastPost = _db.Posts
                    .ToList()
                    .OrderBy(p => p.Publish.ToUnixTimeSeconds())
                    .LastOrDefault();
                if (lastPost != null)
                {
                    result = lastPost.Url;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message}");
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

        private void RaisePage(Uri url)
        {
            try
            {
                PageChangded.Invoke(this, url);
            }
            catch { }
        }

        private void RaiseError(string error)
        {
            try
            {
                Error.Invoke(this, error);
            }
            catch { }
        }

        private void RaiseReport(string error)
        {
            try
            {
                Report.Invoke(this, error);
            }
            catch { }
        }

        #endregion

        #endregion
    }
}