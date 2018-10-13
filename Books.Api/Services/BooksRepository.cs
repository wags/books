using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Books.Api.Contexts;
using Books.Api.Entities;
using Books.Api.ExternalModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Books.Api.Services
{
    public class BooksRepository : IBooksRepository, IDisposable
    {
        private BooksContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private CancellationTokenSource _cancellationTokenSource;

        public BooksRepository(BooksContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<Book> GetBookAsync(Guid id)
        {
            return await _context.Books.Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Book>> GetBooksAsync()
        {
            await _context.Database.ExecuteSqlCommandAsync("WAITFOR DELAY '00:00:02';");
            return await _context.Books.Include(b => b.Author).ToListAsync();
        }

        public async Task<IEnumerable<Entities.Book>> GetBooksAsync(IEnumerable<Guid> bookIds)
        {
            return await _context.Books.Where(b => bookIds.Contains(b.Id))
                .Include(b => b.Author).ToListAsync();
        }

        public async Task<BookCover> GetBookCoverAsync(string coverId)
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Pass through a dummy name
            var response = await httpClient
                .GetAsync($"http://localhost:52644/api/bookcovers/{coverId}");

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<BookCover>(await response.Content.ReadAsStringAsync());
            }

            return null;
        }

        public async Task<IEnumerable<BookCover>> GetBookCoversAsync(Guid bookId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var bookCovers = new List<BookCover>();
            _cancellationTokenSource = new CancellationTokenSource();

            // Create a list of fake book covers
            var bookCoverUrls = new[]
            {
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover1",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover2?returnFault=true",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover3",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover4",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover5",
            };

            //foreach (var bookCoverUrl in bookCoverUrls)
            //{
            //    var response = await httpClient
            //        .GetAsync(bookCoverUrl);

            //    if (response.IsSuccessStatusCode)
            //    {
            //        bookCovers.Add(JsonConvert.DeserializeObject<BookCover>(
            //            await response.Content.ReadAsStringAsync()));
            //    }
            //}

            // Create the tasks
            var downloadBookCoverTasksQuery =
                from bookCoverUrl
                in bookCoverUrls
                select DownloadBookCoverAsync(httpClient, bookCoverUrl, _cancellationTokenSource.Token);

            // Start the tasks
            var downloadBookCoverTasks = downloadBookCoverTasksQuery.ToList();

            return await Task.WhenAll(downloadBookCoverTasks);
        }

        private async Task<BookCover> DownloadBookCoverAsync(HttpClient httpClient, string bookCoverUrl, CancellationToken cancellationToken)
        {
            var response = await httpClient
                .GetAsync(bookCoverUrl, cancellationToken);

            // If we ever need to manually check for cancellation requested
            // cancellationToken.IsCancellationRequested
            // or
            // cancellationToken.ThrowIfCancellationRequested

            if (response.IsSuccessStatusCode)
            {
                var bookCover = JsonConvert.DeserializeObject<BookCover>(
                    await response.Content.ReadAsStringAsync());
                return bookCover;
            }

            // Trigger a cancellation if the response from the API isn't successful
            _cancellationTokenSource.Cancel();

            return null;
        }

        public IEnumerable<Book> GetBooks()
        {
            _context.Database.ExecuteSqlCommand("WAITFOR DELAY '00:00:02';");
            return _context.Books.Include(b => b.Author).ToList();
        }

        public void AddBook(Book bookToAdd)
        {
            if (bookToAdd == null)
            {
                throw new ArgumentNullException(nameof(bookToAdd));
            }

            _context.Add(bookToAdd);
        }

        public async Task<bool> SaveChangesAsync()
        {
            // Return true if 1 or more entities were changed
            return await _context.SaveChangesAsync() > 0;
        }

        public void Dispose()
        {
            Dispose(true);
            // Ensure that the CLR doesn't call finalize for our repository
            // (tell the garbage collector that this repository has already been cleaned up)
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }
    }
}
