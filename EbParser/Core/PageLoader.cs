using CloudFlareUtilities;
using EbParser.Interfaces;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class PageLoader : IPageLoader, IDisposable
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
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request, CancellationToken.None);
                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    HandleException(new Exception($"Status Code: [{response.StatusCode}]"));
                }
            }
            catch (HttpRequestException httpex)
            {
                HandleException(httpex);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return result;
        }

        #endregion

        #region Private

        private void HandleException(Exception ex, [CallerMemberName]string src = "N/A")
        {
            Debug.WriteLine($"[{src}]: {ex.Message}");
            throw ex;
        }

        #endregion
    }
}