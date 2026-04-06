using Contact.API.Helpers;
using Contact.API.Models;
using Contact.API.Data;
using Contact.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Tests
{
    public class PasswordHasherTests
    {
        [Fact]
        public void Hash_ReturnsNonEmptyString()
        {
            var hash = PasswordHasher.Hash("testpassword");
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void Hash_SamePassword_ReturnsDifferentHashes()
        {
            var hash1 = PasswordHasher.Hash("testpassword");
            var hash2 = PasswordHasher.Hash("testpassword");
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            var password = "mySecret123";
            var hash = PasswordHasher.Hash(password);
            Assert.True(PasswordHasher.Verify(password, hash));
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("correctPassword");
            Assert.False(PasswordHasher.Verify("wrongPassword", hash));
        }

        [Fact]
        public void Verify_EmptyPassword_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("somePassword");
            Assert.False(PasswordHasher.Verify("", hash));
        }
    }

    public class ClientsControllerTests
    {
        private AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Create_ValidClient_ReturnsOk()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientsController>();
            var controller = new ClientsController(db, logger);
            var client = new Client { FullName = "Тест Тестович", Phone = "0991234567", Email = "test@test.com" };
            var result = await controller.Create(client);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_MissingFullName_ReturnsBadRequest()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientsController>();
            var controller = new ClientsController(db, logger);
            var client = new Client { FullName = "", Phone = "0991234567" };
            var result = await controller.Create(client);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ExistingClient_ReturnsNoContent()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientsController>();
            var controller = new ClientsController(db, logger);
            var client = new Client { FullName = "Тест", Phone = "0991234567" };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            var result = await controller.Delete(client.Id);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_NonExistingClient_ReturnsNotFound()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientsController>();
            var controller = new ClientsController(db, logger);
            var result = await controller.Delete(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_NonExistingClient_ReturnsNotFound()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientsController>();
            var controller = new ClientsController(db, logger);
            var result = await controller.Update(999, new Client { FullName = "Новий", Phone = "123" });
            Assert.IsType<NotFoundResult>(result);
        }
    }

    public class UsersControllerTests
    {
        private AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void Register_ValidData_ReturnsOk()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UsersController>();
            var controller = new UsersController(db, logger);
            var result = controller.Register(new RegisterRequest("testuser", "test@test.com", "password123"));
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void Register_DuplicateUsername_ReturnsConflict()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UsersController>();
            var controller = new UsersController(db, logger);
            db.Users.Add(new User { Username = "testuser", Email = "t@t.com", PasswordHash = "hash", Role = "user" });
            db.SaveChanges();
            var result = controller.Register(new RegisterRequest("testuser", "other@test.com", "password123"));
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public void Register_EmptyUsername_ReturnsBadRequest()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UsersController>();
            var controller = new UsersController(db, logger);
            var result = controller.Register(new RegisterRequest("", "test@test.com", "password123"));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Register_AlwaysAssignsUserRole()
        {
            var db = CreateInMemoryDb();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UsersController>();
            var controller = new UsersController(db, logger);
            controller.Register(new RegisterRequest("testuser", "test@test.com", "password123"));
            var user = db.Users.FirstOrDefault(u => u.Username == "testuser");
            Assert.Equal("user", user?.Role);
        }
    }

    public class ServicesControllerTests
    {
        private AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            var db = CreateInMemoryDb();
            var controller = new ServicesController(db);
            var result = await controller.GetAll();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_InvalidCategory_ReturnsBadRequest()
        {
            var db = CreateInMemoryDb();
            var controller = new ServicesController(db);
            var service = new Service { Name = "Тест", Category = "Invalid", Price = 100 };
            var result = await controller.Create(service);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ValidService_ReturnsCreated()
        {
            var db = CreateInMemoryDb();
            var controller = new ServicesController(db);
            var service = new Service { Name = "Ремонт телефону", Category = "Repair", Price = 500 };
            var result = await controller.Create(service);
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Delete_NonExisting_ReturnsNotFound()
        {
            var db = CreateInMemoryDb();
            var controller = new ServicesController(db);
            var result = await controller.Delete(999);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}