using EbParser.Context;
using EbParser.Core;
using EbParser.DTO;
using EbParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                var pages = await ParsePagesCount();
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
                        foreach (var linkTag in linkTags)
                        {
                            try
                            {
                                var postUrl = await _parser.ParseAttributeAsync(linkTag, "a", "href");
                                RaisePage(new Uri(postUrl));
                                if (postUrl == lastUrl)
                                {
                                    isEnd = true;
                                    break;
                                }
                                await ParsePostAsync(postUrl);
                                RaiseReport(new string('-', 40));
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

        private async Task<int> ParsePagesCount()
        {
            var site = await _loader.LoadPageAsync(string.Format(Pattern, 0));
            var pages = await _parser.ParseHtmlAsync(site, EbSelectors.PagesSelector);
            var last = pages.Last();
            var result = await _parser.ParseTextAsync(last, "a");

            return int.Parse(result);
        }

        private async Task ParsePostAsync(string postUrl)
        {
            var stopWatch = Stopwatch.StartNew();

            // Load page

            var pageHtml = await _loader.LoadPageAsync(postUrl);
            RaiseReport($"Page loaded: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            stopWatch.Restart();

            // Parse page

            using var pageParser = new PostParser(pageHtml);
            var post = await pageParser.GetPostDtoAsync();
            post.Url = postUrl;
            var comments = await pageParser.GetPostCommentsAsync();
            RaiseReport($"Page parsed: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            stopWatch.Restart();
            
            // Save page

            await AddToBaseAsync(post, comments);
            RaiseReport($"Page saved: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            stopWatch.Restart();
            if (_saveFiles == true)
            {
                var files = new List<string>() { post.Poster };
                files.AddRange(await pageParser.GetPostFilesAsync());
                await SavePostFiles(files);
                RaiseReport($"Files saved: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            }
            stopWatch.Stop();
        }

        private async Task<IList<string>> GetPostLinksFromPageAsync(string pageUrl)
        {
            var content = await _loader.LoadPageAsync(pageUrl);
            var postTitles = await _parser.ParseHtmlAsync(content, EbSelectors.PostLinkSelector);

            return postTitles;
        }

        #endregion

        #region Saving

        private async Task AddToBaseAsync(PostDto postDto, IList<CommentDto> commentDTOs)
        {
            var stopWatch = Stopwatch.StartNew();
            var tags = ProcessTags(postDto.Tags);
            RaiseReport($"Tags saved: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            stopWatch.Restart();
            var post = await ProcessPost(postDto);
            RaiseReport($"Post saved: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            stopWatch.Restart();
            await ProcessComments(commentDTOs, post);
            RaiseReport($"Comments saved: [{ stopWatch.Elapsed.TotalMilliseconds }]");
            stopWatch.Stop();
        }

        private IList<Tag> ProcessTags(IList<string> tags)
        {
            foreach (var tagName in tags)
            {
                var dbTag = _db.Tags.FirstOrDefault(t => t.Name == tagName);
                if (dbTag == null)
                {
                    var tagModel = new Tag() { Name = tagName };
                    _db.Tags.Add(tagModel);
                }
            }

            _db.SaveChangesAsync();

            return _db.Tags.ToList();
        }

        private async Task<Post> ProcessPost(PostDto postDto)
        {
            var post = new Post()
            {
                Url = postDto.Url,
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
            //await _db.SaveChangesAsync();
            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            return post;
        }

        private async Task<IList<Comment>> ProcessComments(IList<CommentDto> comments, Post post)
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
                    ParentId = null,
                    Updated = DateTime.Now,
                };
                _db.Comments.Add(dbComment);

                await ProcessChildComments(comments, comment, dbComment.Id, post);
            }

            return _db.Comments.ToList();
        }

        private async Task ProcessChildComments(IList<CommentDto> allComments, CommentDto parent, int parentId, Post post)
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
                    ParentId = parentId,
                    Updated = DateTime.Now,
                };
                _db.Comments.Add(dbComment);

                await ProcessChildComments(allComments, child, dbComment.Id, post);
            }
        }

        private async Task SavePostFiles(IList<string> files)
        {
            foreach (var fileSrc in files)
            {
                var name = await SaveSiteFile(fileSrc);
                if (!string.IsNullOrEmpty(name))
                {
                    var file = new Context.File()
                    {
                        Url = fileSrc,
                        FileName = name,
                    };
                    _db.Files.Add(file);
                }
            }
            await _db.SaveChangesAsync();
        }

        private async Task<string> SaveSiteFile(string url)
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

            if (FileExists(url))
            {
                return result;
            }

            var directory = CreateFilesDirecory();
            var ext = Path.GetExtension(fileName);
            var newName = Path.Combine(directory, Guid.NewGuid().ToString() + ext);
            await _loader.LoadFileAsync(url, newName);
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

        private bool FileExists(string url)
        {
            var result = false;

            var inDb = _db.Files.FirstOrDefault(f => f.Url == url);
            if (inDb != null)
            {
                var path = Path.Combine(FileDirectory, inDb.FileName);
                var inDirectory = System.IO.File.Exists(path);

                result = inDirectory;
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