using AngleSharp;
using AngleSharp.Dom;
using EbParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class AngelParser : IHtmlParser, IDisposable
    {
        private readonly IBrowsingContext _browsingContext;

        #region Life

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    (_browsingContext as IDisposable)?.Dispose();
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

        public AngelParser()
        {
            var configuration = Configuration.Default;
            _browsingContext = BrowsingContext.New(configuration);
        }

        #region IHtmlParser Implementation

        /// <summary>
        /// Parse html element
        /// </summary>
        /// <param name="html">Html source</param>
        /// <param name="selector">Element selector</param>
        /// <returns>Colletion of elements content. Emtpy collection if error</returns>
        /// <exception cref="ArgumentNullException"/>
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
            try
            {
                var elements = await GetElements(html, selector);
                foreach (var element in elements)
                {
                    result.Add(element.OuterHtml);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return result;
        }

        /// <summary>
        /// Parse element attribute value
        /// </summary>
        /// <param name="html">Html source</param>
        /// <param name="selector">Element selector</param>
        /// <param name="attribute">Attribute name</param>
        /// <returns>Attribute value, if error null</returns>
        /// <exception cref="ArgumentNullException"/>
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
            try
            {
                var elements = await GetElements(html, selector);
                if (elements.Any())
                {
                    result = elements.First().GetAttribute(attribute);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return result;
        }

        /// <summary>
        /// Parse text from element
        /// </summary>
        /// <param name="html">Html source</param>
        /// <param name="selector">Element selector</param>
        /// <returns>Text of element or null if error</returns>
        /// <exception cref="ArgumentNullException"/>
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
            try
            {
                var elements = await GetElements(html, selector);
                if (elements.Any())
                {
                    result = elements.First().TextContent;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return result;
        }

        #endregion

        #region Private

        private async Task<IList<IElement>> GetElements(string html, string selector)
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

        private void HandleException(Exception ex, [CallerMemberName]string src = "N/A")
        {
            Debug.WriteLine($"{src}: {ex.Message}");
            throw ex;
        }

        #endregion
    }
}
