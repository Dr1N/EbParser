using CloudflareSolverRe;
using EbParser.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EbParser.Core
{
    internal class PageLoader : ILoader, IDisposable
    {
        #region Fields

        private readonly ClearanceHandler _clearanceHandler;
        private readonly HttpClient _httpClient;

        #endregion

        #region Life

        public PageLoader()
        {
            _clearanceHandler = new ClearanceHandler
            {
                MaxTries = 5,
                ClearanceDelay = 3000
            };

            _httpClient = new HttpClient(_clearanceHandler, true);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                throw new ArgumentException(nameof(url));
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, CancellationToken.None).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                throw new HttpRequestException($"Status Code: { response.StatusCode }");
            }
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

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, CancellationToken.None).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var fsStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                await contentStream.CopyToAsync(fsStream).ConfigureAwait(false);

                return true;
            }
            else
            {
                throw new HttpRequestException(response.StatusCode.ToString());
            }
        }

        #endregion
    }
}