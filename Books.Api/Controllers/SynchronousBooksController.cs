using System;
using Books.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Books.Api.Controllers
{
    [Route("api/synchronousbooks")]
    public class SynchronousBooksController : ControllerBase
    {
        private IBooksRepository _booksRepository;

        public SynchronousBooksController(IBooksRepository booksRepository)
        {
            _booksRepository = booksRepository ??
                throw new ArgumentNullException(nameof(booksRepository));
        }

        [HttpGet]
        public IActionResult GetBooks()
        {
            //var bookEntities = _booksRepository.GetBooks();
            var bookEntities = _booksRepository.GetBooksAsync().Result;

            // Alternatively:
            //_booksRepository.GetBooksAsync().Wait();

            return Ok(bookEntities);
        }
    }
}
