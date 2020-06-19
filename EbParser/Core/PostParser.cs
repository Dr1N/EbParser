using AngleSharp;
using AngleSharp.Dom;
using EbParser.DTO;
using EbParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EbParser.Core
{
    internal class PostParser : IPostParser, IDisposable
    {
        #region Fields

        private readonly string _html;
        private readonly IBrowsingContext _browsingContext;
        private IDocument _document;

        #endregion

        #region Life

        public PostParser(string html)
        {
            _html = html ?? throw new ArgumentNullException();
            _browsingContext = BrowsingContext.New(Configuration.Default);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _document?.Dispose();
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

        #region IPostParser implementation

        public async Task<PostDto> GetPostDtoAsync()
        {
            var result = new PostDto();
            var document = await GetDocumentAsync().ConfigureAwait(false);
            var title = document.QuerySelector(EbSelectors.PostTitleSelector).TextContent;
            var author = document.QuerySelector(EbSelectors.PostAuthorSelector).TextContent;
            var dateTime = document.QuerySelector(EbSelectors.PostTimeSelector).GetAttribute("datetime");
            var poster = document.QuerySelector(EbSelectors.PostPosterSelector).GetAttribute("src");
            var content = document.QuerySelectorAll(EbSelectors.PostContentSelector).FirstOrDefault().OuterHtml;
            var category = document.QuerySelector(EbSelectors.PostCategorytSelector).TextContent;
            var tags = await GetPostTagsAsync().ConfigureAwait(false);

            result.Title = title;
            result.Author = author;
            result.Publish = DateTimeOffset.Parse(dateTime).DateTime;
            result.Poster = poster;
            result.Content = content;
            result.Category = category;
            result.Tags = tags;

            return result;
        }

        public async Task<IList<CommentDto>> GetPostCommentsAsync()
        {
            var result = new List<CommentDto>();
            var document = await GetDocumentAsync().ConfigureAwait(false);
            var commentsList = document.QuerySelectorAll(EbSelectors.PostCommentListSelector);

            foreach (var comment in commentsList)
            {
                try
                {
                    var commentDto = GetCommentFromItemAsync(comment);
                    var parent = FindParentAsync(comment, "li");
                    if (parent != null)
                    {
                        var parentId = parent.QuerySelector(EbSelectors.PostCommentIdSelector).GetAttribute("id");
                        commentDto.ParrentId = int.Parse(parentId.Split('-').Last());
                    }

                    result.Add(commentDto);
                }
                catch { }
            }

            return result;
        }

        public async Task<IList<string>> GetPostFilesAsync()
        {
            var result = new List<string>();
            var document = await GetDocumentAsync().ConfigureAwait(false);
            var poster = document.QuerySelector(EbSelectors.PostPosterSelector).GetAttribute("src");
            result.Add(poster);
            var content = document.QuerySelectorAll(EbSelectors.PostContentSelector).FirstOrDefault();
            var images = content.QuerySelectorAll("img");
            images.ToList().ForEach(i => result.Add(i.GetAttribute("src")));
            foreach (var image in images)
            {
                result.Add(image.GetAttribute("src"));
            }

            return result;
        }

        #endregion

        #region Private

        private async Task<IDocument> GetDocumentAsync()
        {
            if (_document == null)
            {
                _document = await _browsingContext.OpenAsync(req => req.Content(_html)).ConfigureAwait(false);
            }

            return _document;
        }

        private async Task<IList<string>> GetPostTagsAsync()
        {
            var result = new List<string>();
            var document = await GetDocumentAsync().ConfigureAwait(false);
            var tags = document.QuerySelectorAll(EbSelectors.PostTagstSelector);
            foreach (var tag in tags)
            {
                try
                {
                    result.Add(tag.QuerySelector("a").TextContent);
                }
                catch { }
            }

            return result;
        }

        private CommentDto GetCommentFromItemAsync(IElement commentElement)
        {
            var id = commentElement.QuerySelector(EbSelectors.PostCommentIdSelector).GetAttribute("id");
            var author = commentElement.QuerySelector(EbSelectors.PostCommentAuthorSelector).TextContent;
            var publish = commentElement.QuerySelector(EbSelectors.PostCommentDateSelector).TextContent;
            var content = commentElement.QuerySelector(EbSelectors.PostCommentContentSelector).TextContent;

            if (int.TryParse(id.Split('-').Last(), out int idInt))
            {
                return new CommentDto()
                {
                    Id = idInt,
                    Author = author,
                    Publish = ParseCommentPublishTime(publish),
                    Content = content,
                };
            }

            return null;
        }

        private IElement FindParentAsync(IElement element, string parent = null)
        {
            IElement result = null;
            var parentElement = element.ParentElement;
            while (true)
            {
                if (parentElement == null
                    || string.Compare(parentElement.TagName, "body") == 0
                    || string.Compare(parentElement.TagName, "html") == 0)
                {
                    break;
                }
                if (!string.IsNullOrEmpty(parent))
                {
                    if (string.Compare(parentElement.TagName, parent, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = parentElement;
                        break;
                    }
                    else
                    {
                        parentElement = parentElement.ParentElement;
                    }
                }
                else
                {
                    result = element.ParentElement;
                    break;
                }
            }

            return result;
        }

        private DateTime ParseCommentPublishTime(string publish)
        {
            var dateTimeStr = publish.Replace("в", "");
            var result = DateTime.Parse(string.Join(' ', dateTimeStr));

            return result;
        }

        #endregion
    }
}