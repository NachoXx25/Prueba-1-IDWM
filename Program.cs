using System.IO.Compression;
using ebooks_dotnet8_api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("ebooks"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

var ebooks = app.MapGroup("api/ebook");

// TODO: Add more routes
ebooks.MapPost("/", CreateEBook);
ebooks.MapGet("/", GetAvailableEBooks);
ebooks.MapPut("/{id}", UpdateEBook);
ebooks.MapPut("/{id}/change-availability", ChangeAvailability);
ebooks.MapPut("/{id}/increment-stock", IncrementStock);
ebooks.MapPost("/purchase", MakePurchase);
ebooks.MapDelete("/{id}", DeleteEBook);
app.Run();

// TODO: Add more methods
async Task<IResult> CreateEBook(DataContext db, [FromBody] EBookCreateDTO eBook)
{
    if(eBook.Title == string.Empty || eBook.Author == string.Empty || eBook.Genre == string.Empty || eBook.Format == string.Empty || eBook.Price <= 0){
        return Results.BadRequest("Debe ingresar la totalidad de datos de un eBook");
    }
    if(db.EBooks.FirstOrDefault(e => e.Title == eBook.Title) != null && db.EBooks.FirstOrDefault(e => e.Author == eBook.Author) != null ){
        return Results.Conflict("El ebook ya se encuentra registrado");
    }
    var newEBook = new EBook {
            Title = eBook.Title,
            Author = eBook.Author,
            Genre = eBook.Genre,
            Format = eBook.Format,
            Price = eBook.Price,
            IsAvailable = true,
            Stock = 0
        };
        db.Add(newEBook);
        db.SaveChanges();
        return Results.Ok(newEBook);
}


async Task<IResult> GetAvailableEBooks(DataContext db, [FromQuery] string? genre, [FromQuery] string? author, [FromQuery] string? format){
    
    if(genre != null && author == null && format == null){
        return Results.Ok(db.EBooks.Where(E => E.Genre == genre).OrderBy(e => e.Title).ToList());
    }
    if(author != null && genre == null && format == null){
        return Results.Ok(db.EBooks.Where(E => E.Author == author).OrderBy(e => e.Title).ToList());
    }
    if(format != null && author == null && genre == null){
        return Results.Ok(db.EBooks.Where(E => E.Format == format).OrderBy(e => e.Title).ToList());
    }

    return Results.Ok(db.EBooks.Where(e => e.IsAvailable == true).OrderBy(e => e.Title).ToList());

}


async Task <IResult> UpdateEBook(DataContext db, int id, [FromBody] EBookUpdateDTO eBooks){

    var eBook = db.EBooks.Find(id);

    if(eBook != null){
        eBook.Title = eBooks.Title ?? eBook.Title;
        eBook.Author = eBooks.Author ?? eBook.Author;
        eBook.Genre = eBooks.Genre ?? eBook.Genre;
        eBook.Format = eBooks.Format ?? eBook.Format;
        if(eBooks.Price != null){
            eBook.Price = eBooks.Price;
        }
        db.SaveChanges();
        return Results.Ok(eBook);
    }
    return Results.NotFound();
}


async Task<IResult> ChangeAvailability(DataContext db, int id){

    var eBook = db.EBooks.Find(id);

    if(eBook != null){
        eBook.IsAvailable = !eBook.IsAvailable;
        db.SaveChanges();
        return Results.Ok(eBook);
    }
    return Results.NotFound();
}

async Task<IResult> MakePurchase(DataContext db, [FromBody] EBookPurchaseDTO eBookPurchaseDTO){
    if(eBookPurchaseDTO.Id <= 0 || eBookPurchaseDTO.Quantity <= 0 || eBookPurchaseDTO.Amount <=0){
        return Results.BadRequest("Error al proporcionar datos de compra");
    }
    var eBook = db.EBooks.Find(eBookPurchaseDTO.Id);

    if(eBook != null && eBook.IsAvailable != false){
        if(eBook.Stock > eBookPurchaseDTO.Quantity && eBook.Price * eBookPurchaseDTO.Quantity == eBookPurchaseDTO.Amount){
            eBook.Stock -= eBookPurchaseDTO.Quantity;
            db.SaveChanges();
            return Results.Ok("Venta realizada!");
        }
        return Results.BadRequest("Los datos proporcionados no son correctos");
    }
    return Results.NotFound();
}

async Task<IResult> IncrementStock(DataContext db, int id, [FromBody] EBookDTO eBookDTO){

    var eBook = db.EBooks.Find(id);

    if(eBook != null){
        if(eBookDTO.Stock <= 0){
            return Results.BadRequest("Valor de stock no valido");
        }
        eBook.Stock += eBookDTO.Stock;
        db.SaveChanges();
        return Results.Ok(eBook);
    }
    return Results.NotFound();
}


async Task<IResult> DeleteEBook(DataContext db, int id){

    var eBook = db.EBooks.Find(id);

    if(eBook != null){
        db.Remove(eBook);
        db.SaveChanges();
        return Results.NoContent();
    }
    return Results.NotFound();
}

