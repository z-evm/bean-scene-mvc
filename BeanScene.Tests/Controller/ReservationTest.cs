using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeanScene.Controllers;
using BeanScene.Data;
using BeanScene.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeanScene.Tests
{
    [TestFixture]
    public class ReservationControllerTests
    {
        // Mock the UserManager<IdentityUser>
        private UserManager<IdentityUser> MockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            return new Mock<UserManager<IdentityUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!
            ).Object;
        }

        // Create an in-memory database context for testing
        private ApplicationDbContext MockDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Test]
        public void Search_Get_ReturnsViewResult()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ReservationController>>();
            var mockUserManager = MockUserManager();
            var mockContext = MockDbContext();
            var controller = new ReservationController(mockContext, logger, mockUserManager);

            // Act
            var result = controller.Search() as ViewResult;

            // Assert
            Assert.IsNotNull(result, "Expected a non-null ViewResult.");
            Assert.AreEqual(null, result?.ViewName, "Expected default view name to be used.");
        }

        

        [Test]
        public async Task Search_Post_NoAvailableSittings_ReturnsSearchViewWithError()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ReservationController>>();
            var mockUserManager = MockUserManager();
            var mockContext = MockDbContext();
            var controller = new ReservationController(mockContext, logger, mockUserManager);

            // Act
            var result = await controller.Search(DateTime.Now, TimeSpan.FromHours(1), 5) as ViewResult;

            // Assert
            Assert.IsNotNull(result, "Expected a non-null ViewResult.");
            Assert.IsTrue(controller.ModelState.ContainsKey(""), "Expected ModelState to contain an error for key ''.");
            Assert.AreEqual("Search", result?.ViewName, "Expected view name to be 'Search'.");
        }
        [Test]
        public async Task Book_Get_ReturnsBookingViewWithPreFilledReservation()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ReservationController>>();
            var mockUserManager = MockUserManager();
            var mockContext = MockDbContext();

            // Seed data for sittings
            mockContext.Sittings.Add(new Sitting
            {
                Id = 1,
                Name = "Friday Breakfast",
                Start = DateTime.Now.AddHours(-1),
                End = DateTime.Now.AddHours(1),
                Capacity = 10,
                Closed = false,
                Type = SittingType.Breakfast,
                Reservations = new List<Reservation> // Seed with existing reservations
                {
                    new Reservation { Id = 1, Start = DateTime.Now.AddMinutes(-30), End = DateTime.Now.AddMinutes(30), Pax = 2 }
                }
            });

            await mockContext.SaveChangesAsync();

            var controller = new ReservationController(mockContext, logger, mockUserManager);

            // Act
            var result = await controller.Book(1, 5, DateTime.Now) as ViewResult;
            var model = result?.Model as Reservation;

            // Assert
            Assert.IsNotNull(result, "Expected a non-null ViewResult.");
            Assert.IsNotNull(model, "Expected a non-null Reservation model.");
            Assert.AreEqual(5, model.Pax, "Expected the number of guests (Pax) to be 5.");
            Assert.AreEqual(DateTime.Now.Date, model.Start.Date, "Expected reservation date to match today's date.");
        }
    }
}
