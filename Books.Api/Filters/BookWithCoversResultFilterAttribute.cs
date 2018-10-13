using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Books.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Books.Api.Filters
{
    public class BookWithCoversResultFilterAttribute : ResultFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var resultFromAction = context.Result as ObjectResult;
            if (resultFromAction?.Value == null
                || resultFromAction.StatusCode < 200
                || resultFromAction.StatusCode >= 300)
            {
                await next();
                return;
            }

            // Tempting to do this:
            //var (book, bookCovers) = ((Entities.Book book,
            //    IEnumerable<ExternalModels.BookCover> bookCovers))resultFromAction.Value;

            // Cast without any naming
            //var temp = ((Entities.Book, IEnumerable<ExternalModels.BookCover>))resultFromAction.Value;

            // Cleanest way (no need to use the names):
            var (book, bookCovers) = ((Entities.Book,
                IEnumerable<ExternalModels.BookCover>))resultFromAction.Value;

            var mappedBook = Mapper.Map<BookWithCovers>(book);

            resultFromAction.Value = Mapper.Map(bookCovers, mappedBook);

            await next();
        }
    }
}
