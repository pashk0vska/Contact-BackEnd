using Contact.API.Helpers; using Contact.API.Models; using Contact.API.Data; using Contact.API.Controllers; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace Contact.API.Tests
{
    public class PasswordHasherTests
    {
        [Fact] public void Hash_ReturnsNonEmptyString(){Assert.NotEmpty(PasswordHasher.Hash("testpassword"));}
        [Fact] public void Verify_CorrectPassword_ReturnsTrue(){var h=PasswordHasher.Hash("mySecret123");Assert.True(PasswordHasher.Verify("mySecret123",h));}
        [Fact] public void Verify_WrongPassword_ReturnsFalse(){var h=PasswordHasher.Hash("correct");Assert.False(PasswordHasher.Verify("wrong",h));}
    }
    public class ClientsControllerTests
    {
        private AppDbContext CreateInMemoryDb()=>new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        [Fact] public async Task Create_ValidClient_ReturnsOk(){var db=CreateInMemoryDb();var c=new ClientsController(db,new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientsController>());Assert.IsType<OkObjectResult>(await c.Create(new Client{FullName="Тест",Phone="099"}));}
        [Fact] public async Task Delete_NonExisting_ReturnsNotFound(){var db=CreateInMemoryDb();var c=new ClientsController(db,new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientsController>());Assert.IsType<NotFoundResult>(await c.Delete(999));}
    }
}
