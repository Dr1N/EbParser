using EbParser.Context;
using EbParser.Core;
using EbParser.DTO;
using EbParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EbParser
{
    class Parser : IParser, IDisposable
    {
        #region Constants

        private const string Site = "https://ebanoe.it";
        private const string Pattern = "https://ebanoe.it/page/{0}/";

        #endregion

        #region Fields

        private readonly bool _saveFiles;
        private readonly bool _test;
        private readonly IPageLoader _loader;
        private readonly IHtmlParser _parser;

        #endregion

        #region Events

        public event EventHandler<Uri> PageChangded = delegate { };
        public event EventHandler<string> Error = delegate { };
        public event EventHandler<string> Report = delegate { };

        #endregion

        #region Life

        public Parser(bool saveFiles, bool test = false)
        {
            _loader = new PageLoader();
            _parser = new AngelParser();
            _saveFiles = saveFiles;
            _test = test;
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
            if (_test)
            {
                await TestAsync();
                return;
            }

            await ParseSiteAsync();
        }

        #endregion

        #region Private

        private async Task TestAsync()
        {
            await Task.Delay(100);
            Console.WriteLine("Hello test");
        }

        #region Parsing

        private async Task<int> ParsePagesCount()
        {
            var site = await _loader.LoadPageAsync(Site);
            var pages = await _parser.ParseHtmlAsync(site, EbSelectors.PagesSelector);
            var last = pages.Last();
            var result = await _parser.ParseTextAsync(last, "a");

            return int.Parse(result);
        }

        private async Task ParseSiteAsync()
        {
            var p = await ParsePagesCount();

            var pageUrl = string.Format(Pattern, 1);
            RaisePage(new Uri(pageUrl));
            try
            {
                var linkTags = await GetPostLinksFromPageAsync(pageUrl);
                RaiseReport($"Find: {linkTags.Count} posts");
                foreach (var linkTag in linkTags)
                {
                    try
                    {
                        await ParsePostAsync(linkTag);
                        break;
                    }
                    catch (Exception ex)
                    {
                        RaiseError(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError(ex.Message);
            }
        }

        private async Task ParsePostAsync(string linkTag)
        {
            var postLink = await _parser.ParseAttributeAsync(linkTag, "a", "href");
            RaisePage(new Uri(postLink));
            var pageHtml = await _loader.LoadPageAsync((string)postLink);
            RaiseReport("Page loaded...");
            var post = await GetPostDtoAsync(pageHtml);
            post.Url = postLink;
            var comments = await GetPostCommentsAsync(pageHtml);
            RaiseReport("Page parsed...");
            await SaveToBaseAsync(post, comments);
            RaiseReport("Saved post");
            //TODO: Save Files
        }

        private async Task<IList<string>> GetPostLinksFromPageAsync(string pageUrl)
        {
            var content = await _loader.LoadPageAsync(pageUrl);
            var postTitles = await _parser.ParseHtmlAsync(content, EbSelectors.PostLinkSelector);

            return postTitles;
        }

        private async Task<PostDto> GetPostDtoAsync(string pageHtml)
        {
            var result = new PostDto();
            var title = await _parser.ParseTextAsync(pageHtml, EbSelectors.PostTitleSelector);
            var author = await _parser.ParseTextAsync(pageHtml, EbSelectors.PostAuthorSelector);
            var dateTime = await _parser.ParseAttributeAsync(pageHtml, EbSelectors.PostTimeSelector, "datetime");
            var poster = await _parser.ParseAttributeAsync(pageHtml, EbSelectors.PostPosterSelector, "src");
            var content = (await _parser.ParseHtmlAsync(pageHtml, EbSelectors.PostContentSelector)).FirstOrDefault();
            var category = await _parser.ParseTextAsync(pageHtml, EbSelectors.PostCategorytSelector);
            var tags = await GetPostTagsAsync(pageHtml);

            result.Title = title;
            result.Author = author;
            result.Publish = DateTimeOffset.Parse(dateTime).DateTime;
            result.Poster = poster;
            result.Content = content;
            result.Category = category;
            result.Tags = tags;

            return result;
        }

        private async Task<IList<string>> GetPostTagsAsync(string pageHtml)
        {
            var result = new List<string>();

            var tags = await _parser.ParseHtmlAsync(pageHtml, EbSelectors.PostTagstSelector);
            foreach (var tag in tags)
            {
                var tagName = await _parser.ParseTextAsync(tag, "a");
                result.Add(tagName);
            }

            RaiseReport($"Find tags: {result.Count}");

            return result;
        }

        private async Task<IList<CommentDto>> GetPostCommentsAsync(string pageHtml)
        {
            var result = new List<CommentDto>();

            var commentContainer = (await _parser.ParseHtmlAsync(pageHtml, EbSelectors.PostCommentContainerSelector)).FirstOrDefault();
            var commentsList = await _parser.ParseHtmlAsync(pageHtml, EbSelectors.PostCommentListSelector);
            
            // Find comments
            
            foreach (var comment in commentsList)
            {
                result.Add(await GetCommentFromItemAsync(comment));
            }

            // Find parents for comments

            foreach (var item in result)
            {
                var parent = await GetCommentParent(commentContainer, item.Id);
                if (parent != null)
                {
                    var parentId = await _parser.ParseAttributeAsync(parent, EbSelectors.PostCommentIdSelector, "id");
                    item.ParrentId = int.Parse(parentId.Split('-').Last());
                }
            }

            RaiseReport($"Find comments: {result.Count}");

            return result;
        }

        private async Task<CommentDto> GetCommentFromItemAsync(string commentItemHtml)
        {
            var id = await _parser.ParseAttributeAsync(commentItemHtml, EbSelectors.PostCommentIdSelector, "id");
            var author = await _parser.ParseTextAsync(commentItemHtml, EbSelectors.PostCommentAuthorSelector);
            var publish = await _parser.ParseTextAsync(commentItemHtml, EbSelectors.PostCommentDateSelector);
            var content = await _parser.ParseTextAsync(commentItemHtml, EbSelectors.PostCommentContentSelector);

            return new CommentDto()
            {
                Id = int.Parse(id.Split('-').Last()),
                Author = author,
                Publish = ParseCommentPublishTime(publish),
                Content = content,
            };
        }

        private DateTime ParseCommentPublishTime(string publish)
        {
            var dateTimeStr = publish.Replace("в", "");
            var result = DateTime.Parse(string.Join(' ', dateTimeStr));

            return result;
        }

        private async Task<string> GetCommentParent(string container, int id)
        {
            var selector = string.Format(EbSelectors.PostCommentParentPattern, id);
            var result = await _parser.FindParentAsync(container, selector, "li");

            return result;
        }

        #endregion

        #region Saving

        private async Task SaveToBaseAsync(PostDto postDto, IList<CommentDto> commentDTOs)
        {
            using var db = new SiteContext();
            var tags = await ProcessTags(db, postDto.Tags);
            var post = await ProcessPost(db, postDto);
            var comments = await ProcessComments(db, commentDTOs, post);
        }

        private async Task<IList<Tag>> ProcessTags(SiteContext db, IList<string> tags)
        {
            foreach (var tag in tags)
            {
                var dbTag = db.Tags.FirstOrDefault(t => t.Name == tag);
                if (dbTag == null)
                {
                    db.Tags.Add(new Tag() { Name = tag });
                }
            }
            await db.SaveChangesAsync();

            return db.Tags.ToList();
        }

        private async Task<Post> ProcessPost(SiteContext db, PostDto postDto)
        {
            var post = new Post()
            {
                Url = postDto.Url,
                Title = postDto.Title,
                Publish = postDto.Publish,
                Content = postDto.Content,
                Category = postDto.Category,
                Updated = DateTime.Now,
            };
            foreach (var tag in postDto.Tags)
            {
                var dbTag = db.Tags.FirstOrDefault(t => t.Name == tag);
                var postTag = new PostTag() { Post = post, Tag = dbTag };
                db.PostTags.Add(postTag);
            }
            db.Posts.Add(post);
            await db.SaveChangesAsync();

            return post;
        }

        private async Task<IList<Comment>> ProcessComments(SiteContext db, IList<CommentDto> comments, Post post)
        {
            var firstLevelComments = comments.Where(c => c.ParrentId == 0).ToList();
            foreach (var comment in firstLevelComments)
            {
                var dbComment = new Comment()
                {
                    Author = comment.Author,
                    Publish = comment.Publish,
                    PostId = post.Id,
                    Content = comment.Content,
                    ParentId = null,
                    Updated = DateTime.Now,
                };
                db.Comments.Add(dbComment);
                await db.SaveChangesAsync();

                await SaveChildComments(db, comments, comment, dbComment.Id, post.Id);
            }

            return db.Comments.ToList();
        }

        private async Task SaveChildComments(SiteContext db, IList<CommentDto> allComments, CommentDto parent, int parentId, int postId)
        {
            var children = allComments.Where(c => c.ParrentId == parent.Id).ToList();
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
                    PostId = postId,
                    Content = child.Content,
                    ParentId = parentId,
                    Updated = DateTime.Now,
                };
                db.Comments.Add(dbComment);
                await db.SaveChangesAsync();

                await SaveChildComments(db, allComments, child, dbComment.Id, postId);
            }
        }

        #endregion

        #region Helpers

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