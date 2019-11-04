using EbParser.Core;
using EbParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EbParser
{
    class Parser : IParser, IDisposable
    {
        #region Constants

        private const string Pattern = "https://ebanoe.it/page/{0}/";

        #endregion

        #region Fields

        private readonly ILoader _loader;
        private readonly IHtmlParser _parser;
        private readonly IPostStorage _storage;
        private readonly bool _saveFiles;

        #endregion

        #region Events

        public event EventHandler<Uri> PageChangded = delegate { };
        public event EventHandler<string> Error = delegate { };
        public event EventHandler<string> Report = delegate { };

        #endregion

        #region Life

        public Parser(bool saveFiles)
        {
            _loader = new PageLoader();
            _parser = new AngelParser();
            _storage = new PostStorage(_loader);
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
                var lastUrl = _storage.GetLastPostUrl();
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
                                await _storage.SaveTagsAsync(postDto.Tags);
                                var postModel = await _storage.SavePostAsync(postDto, postUrl);
                                RaiseReport($"Page saved: { stopWatch.Elapsed.TotalMilliseconds } ms");
                                stopWatch.Restart();
                                var commentDto = await postParser.GetPostCommentsAsync();
                                await _storage.SaveCommentsAsync(commentDto, postModel);
                                RaiseReport($"Comments saved: { stopWatch.Elapsed.TotalMilliseconds } ms");
                                var files = await postParser.GetPostFilesAsync();
                                stopWatch.Restart();
                                if (_saveFiles)
                                {
                                    await _storage.SavePostFilesAsync(files);
                                    RaiseReport($"Fies saved: { stopWatch.Elapsed.TotalMilliseconds } ms");
                                }
                            }
                            catch (Exception ex)
                            {
                                RaiseError(ex.Message);
                            }
                        }
                        //break; // TODO: debug
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

        private async Task<IList<string>> GetPostLinksFromPageAsync(string pageUrl)
        {
            var content = await LoadPageAsync(pageUrl);
            var postTitles = await _parser.ParseHtmlAsync(content, EbSelectors.PostLinkSelector);

            return postTitles;
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