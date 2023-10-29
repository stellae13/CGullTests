using Azure;
using CGullProject;
using CGullProject.Data;
using CGullProject.Models;
using CGullProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Security.Cryptography.Xml;
using Xunit.Abstractions;

namespace CGullTest2
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

    public class UnitTest1 : IClassFixture<TestDatabaseFixture>
    {

        public TestDatabaseFixture Fixture { get; } = new TestDatabaseFixture();

        [Fact]
        public void CreateItemController()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);

            //Act
            var controller = new CGullProject.Controllers.ItemController(service);

            //Assert
            Assert.NotNull(controller);

        }
        [Fact]
        public void CreateCartController()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);

            //Act
            var controller = new CGullProject.Controllers.CartController(service);

            //Assert
            Assert.NotNull(controller);

        }

        [Fact] 
        public async Task GetCartTest()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
       
            //Act
            var res = await controller.GetCart(cartId.First());

            OkObjectResult objectResponse = Assert.IsType<OkObjectResult>(res);
            var cart = objectResponse.Value;

            //Assert
            Assert.NotNull(res);
            Assert.NotNull(cart);
            Assert.True(res is OkObjectResult);
        }

        [Fact]
        public async Task AddTooManyOfAnItemToCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000001";
            var quantity = 21;

            //Act 
            var result = await controller.AddItemToCart(cartId.First(), itemId, quantity);

            //Assert
            Assert.NotNull(result);
            Assert.False(result is OkObjectResult);

        }
        [Fact]
        public async Task AddItemToInvalidCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = new Guid();
            var itemId = "000001";
            var quantity = 21;

            //Act 
            var result = await controller.AddItemToCart(cartId, itemId, quantity);

            //Assert
            Assert.NotNull(result);
            Assert.False(result is OkObjectResult);

        }

        [Fact]
        public async Task AddAnItemToCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
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
            Assert.NotNull(result);
            Assert.NotNull(cartIt);
            Assert.Equal(itemId, cartIt.First().InventoryId); // this line fails
            Assert.Equal(0,item.First().Stock); // this line fails 
            Assert.True(result is Microsoft.AspNetCore.Mvc.OkObjectResult);
        }

        [Fact]
        public async Task GetAllItemsTest()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var controller = new CGullProject.Controllers.ItemController(service);
            List<Inventory> inventory = new List<Inventory>
            {
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
                }
            };

            //Act
            var res = await controller.GetAllItems();
            OkObjectResult objectResponse = Assert.IsType<OkObjectResult>(res);
            var obj = objectResponse.Value;
            
            //Assert
            Assert.NotNull(res);
            Assert.NotNull(obj);
            Assert.Equal(inventory,obj);
            Assert.True(res is OkObjectResult);
        }

        [Fact]
        public async Task CheckoutValidCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000001";
            var quantity = 20;
            await controller.AddItemToCart(cartId.First(), itemId, quantity);


            var _cartId = cartId.First();
            var cardNumber = "5105105105105100";
            var theDate = new DateOnly(2030, 10, 21);
            var cardHolderName = "Stella Garcia";
            var cvv = "134";  
            
            //Act 
            var result = await controller.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.True(result is OkResult);
        }

        [Fact]
        public async Task CheckoutInvalidCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = from b in context.Cart
                          where b.Name == "Stella"
                          select b.Id;
            var itemId = "000001";
            var quantity = 20;
            await controller.AddItemToCart(cartId.First(), itemId, quantity);


            var _cartId = cartId.First();
            var cardNumber = "5105105105100";
            var theDate = new DateOnly(2030, 10, 21);
            var cardHolderName = "Stella Garcia";
            var cvv = "13";

            //Act 
            var result = await controller.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.False(result is OkResult);
        }
        [Fact]
        public async Task CheckoutExpiredCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000001";
            var quantity = 20;
            await controller.AddItemToCart(cartId.First(), itemId, quantity);


            var _cartId = cartId.First();
            var cardNumber = "5105105105105100";
            var theDate = new DateOnly(2020, 10, 21);
            var cardHolderName = "Stella Garcia";
            var cvv = "133";

            //Act 
            var result = await controller.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.False(result is OkResult);
        }

        [Fact]
        public async Task CheckoutInvalidCVVCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000001";
            var quantity = 20;
            await controller.AddItemToCart(cartId.First(), itemId, quantity);


            var _cartId = cartId.First();
            var cardNumber = "5105105105105100";
            var theDate = new DateOnly(2025, 10, 21);
            var cardHolderName = "Stella Garcia";
            var cvv = "13";

            //Act 
            var result = await controller.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.False(result is OkResult);
        }

        [Fact]
        public void BundleTest()
        {
            //Arrange

            //Act 

            //Assert
            Assert.True(true);
        }

        //end of Project phase 1 tests 

        [Fact]
        public void AddItemToInventory() // will be implemented during project phase 2 
        {
            //Arrange

            //Act 

            //Assert
            Assert.True(true);

        }

        [Fact]
        public void RemoveItemFromInventory() // will be implemented during project phase 2 
        {
            //Arrange

            //Act 

            //Assert
            Assert.True(true);

        }
        [Fact]
        public void LoginTest() // will be implemented during project phase 2 
        {
            //Arrange

            //Act 

            //Assert
            Assert.True(true);
        }

    }
}