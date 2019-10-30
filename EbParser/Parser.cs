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

        private const string Pattern = "https://ebanoe.it/page/{0}/";

        private const string PostLinkSelector = "h3.entry-title a";
        private const string PostTitleSelector = "h1.entry-title";
        private const string PostAuthorSelector = "div.author-date a";
        private const string PostTimeSelector = "time.entry-date";
        private const string PostPosterSelector = "div.section-post-header img.wp-post-image";
        private const string PostContentSelector = "div.the_content";
        private const string PostCategorytSelector = "div.category a";
        private const string PostTagstSelector = "ul.post-tags li";

        private const string PostCommentContainerSelector = "ol.commentlist";
        private const string PostCommentListSelector = "ol.commentlist li.comment";
        private const string PostCommentParentPattern = "li#comment-{0}";
        private const string PostCommentAuthorSelector = "cite.fn";
        private const string PostCommentDateSelector = "div.commentmetadata";
        private const string PostCommentIdSelector = "div.comment-body";
        private const string PostCommentContentSelector = "p";

        #endregion

        #region Fields

        private IPageLoader _loader;
        private IHtmlParser _parser;

        #endregion

        #region Events

        public event EventHandler<Uri> PageChangded = delegate { };

        public event EventHandler<string> Error = delegate { };

        #endregion

        #region Life

        public Parser()
        {
            _loader = new PageLoader();
            _parser = new AngelParser();
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
            var pageUrl = string.Format(Pattern, 0);
            if (Uri.TryCreate(pageUrl, UriKind.Absolute, out Uri uri))
            {
                RaisePage(uri);
                try
                {
                    var postTitles = await GetPostFromPage(pageUrl);
                    foreach (var title in postTitles)
                    {
                        try
                        {
                            var postLink = await _parser.ParseAttributeAsync(title, "a", "href");

                            if (string.IsNullOrEmpty(postLink)) continue;

                            RaisePage(new Uri(postLink));
                            var pageHtml = await _loader.LoadPageAsync(postLink);

                            if (string.IsNullOrEmpty(pageHtml)) continue;

                            var post = await GetPostDtoAsync(pageHtml);
                            var comments = await GetPostCommentsAsync(pageHtml);

                            await SaveToBase(post, comments);

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
        }

        #endregion

        #region Private

        private async Task<IList<string>> GetPostFromPage(string pageUrl)
        {
            var content = await _loader.LoadPageAsync(pageUrl);
            var postTitles = await _parser.ParseHtmlAsync(content, PostLinkSelector);

            return postTitles;
        }

        private async Task SaveToBase(PostDto post, IList<CommentDto> comments)
        {
            using (var db = new SiteContext())
            {
                await ProcessTags(db, post.Tags);

            }
            await SavePost(post);
            await SaveComments(post, comments);
        }

        private async Task ProcessTags(SiteContext db, IList<string> tags)
        {
            foreach (var tag in tags)
            {
                var dbTag = db.Tag
                if (true)
                {

                }
            }
        }

        private async Task<PostDto> GetPostDtoAsync(string pageHtml)
        {
            var result = new PostDto();
            var title = await _parser.ParseTextAsync(pageHtml, PostTitleSelector);
            var author = await _parser.ParseTextAsync(pageHtml, PostAuthorSelector);
            var dateTime = await _parser.ParseAttributeAsync(pageHtml, PostTimeSelector, "datetime");
            var poster = await _parser.ParseAttributeAsync(pageHtml, PostPosterSelector, "src");
            var content = (await _parser.ParseHtmlAsync(pageHtml, PostContentSelector)).FirstOrDefault();
            var category = await _parser.ParseTextAsync(pageHtml, PostCategorytSelector);
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

            var tags = await _parser.ParseHtmlAsync(pageHtml, PostTagstSelector);
            foreach (var tag in tags)
            {
                var tagName = await _parser.ParseTextAsync(tag, "a");
                result.Add(tagName);
            }

            return result;
        }

        private async Task<IList<CommentDto>> GetPostCommentsAsync(string pageHtml)
        {
            var result = new List<CommentDto>();

            var commentContainer = (await _parser.ParseHtmlAsync(pageHtml, PostCommentContainerSelector)).FirstOrDefault();
            var commentsList = await _parser.ParseHtmlAsync(pageHtml, PostCommentListSelector);
            
            // Find comments
            
            foreach (var comment in commentsList)
            {
                result.Add(await GetCommentFromItem(comment));
            }

            // Find parents for comments

            foreach (var item in result)
            {
                var parent = await GetCommentParent(commentContainer, item.Id);
                if (parent != null)
                {
                    var parentId = await _parser.ParseAttributeAsync(parent, PostCommentIdSelector, "id");
                    item.ParrentId = int.Parse(parentId.Split('-').Last());
                }
            }

            return result;
        }

        private async Task<CommentDto> GetCommentFromItem(string commentItemHtml)
        {
            var id = await _parser.ParseAttributeAsync(commentItemHtml, PostCommentIdSelector, "id");
            var author = await _parser.ParseTextAsync(commentItemHtml, PostCommentAuthorSelector);
            var publish = await _parser.ParseTextAsync(commentItemHtml, PostCommentDateSelector);
            var content = await _parser.ParseTextAsync(commentItemHtml, PostCommentContentSelector);

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
            var selector = string.Format(PostCommentParentPattern, id);
            var result = await _parser.FindParentAsync(container, selector, "li");

            return result;
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

        #endregion
    }
}