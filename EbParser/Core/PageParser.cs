using AngleSharp;
using AngleSharp.Dom;
using EbParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class AngelParser : IHtmlParser
    {
        #region Fields

        private readonly IBrowsingContext _browsingContext;
        
        #endregion

        #region Life

        public AngelParser()
        {
            _browsingContext = BrowsingContext.New(Configuration.Default);
        }

        #endregion

        #region IHtmlParser Implementation

        public async Task<IList<string>> ParseHtmlAsync(string html, string selector)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentNullException(nameof(html));
            }
            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentNullException(nameof(selector));
            }

            var result = new List<string>();
            var elements = await GetElementsAsync(html, selector);
            foreach (var element in elements)
            {
                result.Add(element.OuterHtml);
            }

            return result;
        }

        public async Task<string> ParseAttributeAsync(string html, string selector, string attribute)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentNullException(nameof(html));
            }
            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentNullException(nameof(selector));
            }
            if (string.IsNullOrEmpty(attribute))
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            string result = null;
            var elements = await GetElementsAsync(html, selector);
            if (elements.Any())
            {
                result = elements.First().GetAttribute(attribute);
            }

            return result;
        }

        public async Task<string> ParseTextAsync(string html, string selector)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentNullException(nameof(html));
            }
            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentNullException(nameof(selector));
            }

            string result = null;
            var elements = await GetElementsAsync(html, selector);
            if (elements.Any())
            {
                result = elements.First().TextContent;
            }

            return result;
        }

        #endregion

        #region Private

        private async Task<IList<IElement>> GetElementsAsync(string html, string selector)
        {
            var result = new List<IElement>();
            var document = await _browsingContext.OpenAsync(req => req.Content(html));
            if (document != null && document.All.Any())
            {
                var elements = document.QuerySelectorAll(selector);
                if (elements?.Length > 0)
                {
                    foreach (var element in elements)
                    {
                        result.Add(element);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}