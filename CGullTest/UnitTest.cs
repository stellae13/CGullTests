using CGullProject;
using CGullProject.Data;
using CGullProject.Models;
using CGullProject.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGet.ContentModel;
using System;
using System.Configuration;
using System.Reflection.Metadata;
using Xunit;

namespace CGullTest
{
    public class TestDatabaseFixture
    {
        private const string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=ShopContext-e2575bc4-b408-4a2e-b97e-73e645144ac8;Trusted_Connection=True;MultipleActiveResultSets=true";
        private static readonly object _lock = new();
        private static bool _databaseInitialized;

        public TestDatabaseFixture() // see https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database for details
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    using (var context = CreateContext())
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();

                       context.Category.AddRange(
                       new Category
                       {
                           Name = "food_and_beverage",

                       });
                       // end of category
                       context.Inventory.AddRange(
                       new Inventory
                       {
                         Id = "000001",
                         Name = "Seagull Drink",
                         CategoryId = 1,
                         MSRP = 1.75M,
                         SalePrice = 1.75M,
                         Rating = 2.6M,
                         Stock = 20
                       },
                       new Inventory
                       {
                         Id = "000002",
                         Name = "Seagull Chips",
                         CategoryId = 1,
                         SalePrice = 5.99M,
                         MSRP = 5.99M,
                         Rating = 4.5M,
                         Stock = 25
                       });
                        //end of inventory 
                        context.Cart.AddRange(
                         new Cart()
                         {
                             Id = Guid.NewGuid(),
                             Name = "Stella"
                         });
                        context.Bundle.AddRange(
                        new Bundle()
                        {
                            Id = "100020",
                            Name = "Food Bundle",
                            Discount = 0.20M,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now + TimeSpan.FromDays(100)
                        });
                        context.BundleItem.AddRange(
                            new BundleItem()
                            {
                                BundleId = "100020",
                                InventoryId = "000001"
                            },
                            new BundleItem()
                            {
                                BundleId = "100020",
                                InventoryId = "000002"
                            }); 

                        context.SaveChanges();
                    }

                    _databaseInitialized = true;
                }
            }

        }
        public ShopContext CreateContext()
        => new ShopContext(
            new DbContextOptionsBuilder<ShopContext>()
                .UseSqlServer(ConnectionString)
                .Options);
    }


    [TestClass]
    public class UnitTest : IClassFixture<TestDatabaseFixture>
    {

        public TestDatabaseFixture Fixture { get; } = new TestDatabaseFixture();

        [TestMethod]
        public void CreateItemController()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var controller = new CGullProject.Controllers.ItemController(service);
            //Act 

            //Assert
            Assert.IsNotNull(controller);

        }
        [TestMethod]
        public void CreateCartController()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var controller = new CGullProject.Controllers.CartController(context);
            //Act 

            //Assert
            Assert.IsNotNull(controller);

        }


        [TestMethod]
        public async Task AddTooManyOfAnItemToCartAsync()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var controller = new CGullProject.Controllers.CartController(context);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000001";
            var quantity = 21;

            //Act 

            var result = await controller.AddItemToCart(cartId.First(), itemId, quantity);
            Console.WriteLine(result);
            //Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result is OkObjectResult);

        }

        [TestMethod]
        public async Task AddAnItemToCart()
        {
            using var context = Fixture.CreateContext();
            var controller = new CGullProject.Controllers.CartController(context);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000001";
            var quantity = 20;

            //Act 
            var result = await controller.AddItemToCart(cartId.First(), itemId, quantity);
            var item = from i in context.Inventory
                       where i.Id == itemId
                       select i;
            var cartIt = from c in context.CartItem
                         where c.CartId == cartId.First()
                         select c;

            //Asserts
            Assert.IsNotNull(result);
            Assert.IsNotNull(cartIt);
            Assert.AreEqual(cartIt.First().InventoryId, itemId);
            Assert.AreEqual(item.First().Stock, 0);
            Assert.IsTrue(result is OkObjectResult);
        }
        [TestMethod]
        public void GetAllItemsTest()
        {
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var controller = new CGullProject.Controllers.ItemController(service);

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void CheckoutValidCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var controller = new CGullProject.Controllers.CartController(context);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;

            //Act 

            //Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void CheckoutInvalidCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var controller = new CGullProject.Controllers.CartController(context);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;

            //Act 

            //Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void BundleTest()
        {
            //Arrange

            //Act 

            //Assert
            Assert.IsTrue(true);
        }

        //end of Project phase 1 tests 

        [TestMethod]
        public void AddItemToInventory() // will be implemented during project phase 2 
        {
            //Arrange

            //Act 

            //Assert
            Assert.IsTrue(true);

        }

        [TestMethod]
        public void RemoveItemFromInventory() // will be implemented during project phase 2 
        {
            //Arrange

            //Act 

            //Assert
            Assert.IsTrue(true);

        }
        [TestMethod]
        public void LoginTest() // will be implemented during project phase 2 
        {
            //Arrange

            //Act 

            //Assert
            Assert.IsTrue(true);
        }

    }
}