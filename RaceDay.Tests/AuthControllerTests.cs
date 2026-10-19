using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Controllers;
using RaceDay.Data;
using RaceDay.DTOs;
using Xunit;

namespace RaceDay.Tests
{
    public class AuthControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsOk()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            var dto = new RegisterDto
            {
                FullName = "Test Organiser",
                Email = "org@test.com",
                Password = "Password123",
                Role = "Organiser"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Register_WithInvalidRole_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            var dto = new RegisterDto
            {
                FullName = "Invalid User",
                Email = "bad@test.com",
                Password = "Password123",
                Role = "Admin" // Invalid role
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}