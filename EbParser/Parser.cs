using EbParser.Core;
using EbParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EbParser
{
    class Parser : IParser, IDisposable
    {
        #region Constants

        private const string PagePattern = "https://ebanoe.it/page/{0}/";
        private const int Attmps = 4;

        #endregion

        #region Fields

        private readonly bool _saveFiles;
        private readonly int _start;
        private readonly IHtmlParser _parser;
        private readonly IPostStorage _storage;
        private ILoader _loader;

        #endregion

        #region Events

        public event EventHandler<Uri> PageChangded = delegate { };
        public event EventHandler<string> Error = delegate { };
        public event EventHandler<string> Report = delegate { };

        #endregion

        #region Life

        public Parser(bool saveFiles, int? startPage = null)
        {
            _saveFiles = saveFiles;
            _start = startPage ?? 0;
            _loader = new PageLoader();
            _parser = new AngelParser();
            _storage = new PostStorage();
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
                    (_storage as IDisposable)?.Dispose();
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
            try
            {
                RaiseReport("START");
                var pages = await ParsePagesCountAsync();   // Site peges count
                RaiseReport($"Pages: { pages }");
                var lastUrl = _storage.GetLastPostUrl();    // Load last parsed post
                RaiseReport($"Last: { lastUrl ?? "New session" }");
                var isEnd = false;
                for (int i = _start; i <= pages; i++)
                {
                    try
                    {
                        if (i == 1) continue;   // Skip first page
                        var pageUrl = string.Format(PagePattern, i);

                        RaisePage(new Uri(pageUrl));
                        var postLinkTags = await GetPostUrlsFromPageAsync(pageUrl);    // Parse post url's from page
                        var stopWatch = Stopwatch.StartNew();
                        foreach (var postUrl in postLinkTags)
                        {
                            try
                            {
                                RaisePage(new Uri(postUrl));
                                if (postUrl == lastUrl)     // Save only new posts
                                {
                                    isEnd = true;
                                    break;
                                }
                                else if (_start != 0 && _storage.IsExists(postUrl))     // Continue loading from page
                                {
                                    continue;
                                }

                                stopWatch.Restart();
                                var html = await LoadPageAsync(postUrl);    // Load post html
                                RaiseReport($"Page loaded: [{ stopWatch.Elapsed.TotalMilliseconds }]");
                                if (string.IsNullOrEmpty(html))
                                {
                                    RaiseError($"Can't load page: { postUrl }");
                                    continue;
                                }
                                stopWatch.Restart();

                                // Parse elements and save to storage

                                using var postParser = new PostParser(html);
                                var postDto = await postParser.GetPostDtoAsync();
                                postDto.Comments = await postParser.GetPostCommentsAsync();
                                postDto.Files = _saveFiles ? await postParser.GetPostFilesAsync() : new List<string>();
                                RaiseReport($"Post parsed: [{ stopWatch.Elapsed.TotalMilliseconds }] ms");
                                stopWatch.Restart();
                                await _storage.SavePostAsync(postUrl, postDto);
                                RaiseReport($"Post saved: { stopWatch.Elapsed.TotalMilliseconds } ms");
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

        #endregion

        #region Private

        #region Parsing

        private async Task<int> ParsePagesCountAsync()
        {
            var mainPageHtml = await LoadPageAsync(string.Format(PagePattern, 0));  // Main page
            var pageLinks = await _parser.ParseHtmlAsync(mainPageHtml, EbSelectors.PagesSelector);
            var lastPage = pageLinks.Last();
            var result = await _parser.ParseTextAsync(lastPage, "a");

            return int.Parse(result);
        }
       
        private async Task<string> LoadPageAsync(string url)
        {
            var result = string.Empty;
            for (int i = 0; i < Attmps; i++)
            {
                try
                {
                    result = await _loader.LoadPageAsync(url);
                    if (!string.IsNullOrEmpty(result))
                    {
                        break;
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.Message.Contains(HttpStatusCode.TooManyRequests.ToString()))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5 + i));      // Wait and continue
                    }
                    else if (ex.Message.Contains(HttpStatusCode.BadRequest.ToString())
                            || ex.Message.Contains(HttpStatusCode.InternalServerError.ToString()))     // Create new loader // TODO
                    {
                        (_loader as IDisposable)?.Dispose();
                        _loader = new PageLoader();
                        await Task.Delay(TimeSpan.FromSeconds(5 + i));
                    }
                }
            }

            return result;
        }

        private async Task<IList<string>> GetPostUrlsFromPageAsync(string pageUrl)
        {
            var result = new List<string>();
            var content = await LoadPageAsync(pageUrl);
            var postTitles = await _parser.ParseHtmlAsync(content, EbSelectors.PostLinkSelector);
            foreach (var postTitle in postTitles)
            {
                var url = await _parser.ParseAttributeAsync(postTitle, "a", "href");
                if (!string.IsNullOrEmpty(url))
                {
                    result.Add(url);
                }
            }

            return result;
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