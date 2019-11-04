using CloudFlareUtilities;
using EbParser.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class PageLoader : ILoader, IDisposable
    {
        #region Fields

        private readonly HttpClientHandler _httpClientHandler;
        private readonly ClearanceHandler _clearanceHandler;
        private readonly HttpClient _httpClient;

        #endregion

        #region Life

        public PageLoader()
        {
            _httpClientHandler = new HttpClientHandler();
            _clearanceHandler = new ClearanceHandler(_httpClientHandler);
            _httpClient = new HttpClient(_clearanceHandler, false);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _httpClientHandler.Dispose();
                    _clearanceHandler.Dispose();
                    _httpClient.Dispose();
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

        #region IPageLoader implentation

        public async Task<string> LoadPageAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException(nameof(url));
            }

            string result = null;
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, CancellationToken.None);
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new HttpRequestException($"Status Code: { response.StatusCode }");
            }

            return result;
        }

        public async Task<bool> LoadFileAsync(string url, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException(nameof(url));
            }

            var result = false;
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, CancellationToken.None);
            if (response.IsSuccessStatusCode)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fsStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                await contentStream.CopyToAsync(fsStream);
                result = true;
            }
            else
            {
                throw new HttpRequestException($"Status Code: { response.IsSuccessStatusCode }");
            }

            return result;
        }

        #endregion
    }
}