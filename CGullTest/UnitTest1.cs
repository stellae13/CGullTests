using Azure;
using CGullProject;
using CGullProject.Data;
using CGullProject.Models;
using CGullProject.Models.DTO;
using CGullProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Linq;
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
            lock (_lock) // make sure tests that run async are not accessing the same data at the same time
            {
                if (!_databaseInitialized)
                {
                    using (var context = CreateContext())
                    {
                        context.Database.EnsureDeleted(); // ensure no database exists
                        context.Database.EnsureCreated(); // ensure one is created 

                        context.Category.AddRange(
                        new Category
                        {
                            Name = "food_and_beverage",

                        });
                        // end of category
                        context.Inventory.AddRange(
                        new Product
                        {
                            Id = "000001",
                            Name = "Seagull Drink",
                            CategoryId = 1,
                            MSRP = 1.75M,
                            SalePrice = 1.75M,
                            Rating = 2.6M,
                            Stock = 20,
                            isBundle = false
                        },
                        new Product
                        {
                            Id = "000002",
                            Name = "Seagull Chips",
                            CategoryId = 1,
                            SalePrice = 5.99M,
                            MSRP = 5.99M,
                            Rating = 4.5M,
                            Stock = 25,
                            isBundle = false
                        },
                        new Product
                        {
                            Id = "000003",
                            Name = "Seagull Ceral",
                            CategoryId = 1,
                            SalePrice = 5.99M,
                            MSRP = 5.99M,
                            Rating = 4.5M,
                            Stock = 25,
                            isBundle = false
                        },
                        new Product
                        {
                            Id = "100020",
                            Name = "Food Bundle",
                            CategoryId = 1,
                            MSRP = 6.00M,
                            SalePrice = 5.00M,
                            Rating = 4.2M,
                            Stock = 20,
                            isBundle = true
                        }); ;
                        //end of inventory 
                        context.Cart.AddRange(
                         new Cart()
                         {
                             Id = Guid.NewGuid(),
                             Name = "Stella"
                         });
                        // end of cart
                        context.CartItem.AddRange(); // no items in cart at beginning of tests 
                        //end of CartItem
                        context.Bundle.AddRange(
                        new Bundle()
                        {
                            ProductId= "100020",
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now + TimeSpan.FromDays(100)
                        });
                        //end of Bundle
                        context.BundleItem.AddRange(
                            new BundleItem()
                            {
                                BundleId = "100020",
                                ProductId = "000001"
                            },
                            new BundleItem()
                            {
                                BundleId = "100020",
                                ProductId = "000002"
                            });
                        //end of bundle items

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

        public UnitTest1(TestDatabaseFixture fixture)
        => Fixture = fixture;

        public TestDatabaseFixture Fixture { get; }

        [Fact]
        public void CreateItemController()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);

            //Act
            var controller = new CGullProject.Controllers.ItemController(service, revService);

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
            var cartStella = from b in context.Cart
                         where b.Name == "Stella"
                         select b;


            //Act
            var res = await controller.GetCart(cartId.First());

            OkObjectResult objectResponse = Assert.IsType<OkObjectResult>(res);
            var cart = objectResponse.Value as CartDTO;
            

            //Assert
            Assert.NotNull(res);
            Assert.NotNull(cart);
            Assert.Equal(cartId.First(), cart.Id);
            Assert.True("Stella" == cart.Name);
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task AddAnItemToCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var cartID = cartId.First();
            var itemId = "000003";
            var quantity = 25;

            //Act 
            var result = await controller.AddItemToCart(cartID, itemId, quantity);
            var item = from i in context.Inventory
                       where i.Id == itemId
                       select i;
            var cartIt = from c in context.CartItem
                         where c.CartId == cartID && c.ProductId == itemId
            select c;

            var cartItem = cartIt.First().ProductId;
            var stock = item.First().Stock;

            //Assert
            Assert.NotNull(result);
            Assert.NotEmpty(cartIt);
            Assert.Equal("000003", cartItem);
            Assert.Equal(0,stock); 
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AddTooManyOfAnItemToCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000001";
            var quantity = 21;

            //Act 
            var result = await controller.AddItemToCart(cartId.First(), itemId, quantity);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkObjectResult>(result);

        }
        [Fact]
        public async Task AddItemToInvalidCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var cartId = new Guid();
            var itemId = "000001";
            var quantity = 19;

            //Act 
            var result = await controller.AddItemToCart(cartId, itemId, quantity);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkObjectResult>(result);

        }

        [Fact]
        public async Task AddAnItemToCartThatIsAlreadyInCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var cartID = cartId.First();
            var itemId = "000002";
            var quantity = 10;
            var result = await controller.AddItemToCart(cartID, itemId, quantity);
            var quantity2 = 5;

            //Act
            var result2 = await controller.AddItemToCart(cartID, itemId, quantity2);

            var item = from i in context.Inventory
                       where i.Id == itemId
                       select i;
            var cartIt = from c in context.CartItem
                         where c.CartId == cartID && c.ProductId == itemId
                         select c;
            var cartItem = cartIt.First().ProductId;
            var stock = item.First().Stock;

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(result2);
            Assert.NotEmpty(cartIt);
            Assert.Equal("000002", cartItem);
            Assert.Equal(10, stock); 
            Assert.IsType<OkObjectResult>(result);
            Assert.IsType<OkObjectResult>(result2);
        }

        [Fact]
        public async Task GetAllItemsTest()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            IEnumerable<Product> inventory = new List<Product>
            {
                new Product
                {
                    Id = "000001",
                    Name = "Seagull Drink",
                    CategoryId = 1,
                    MSRP = 1.75M,
                    SalePrice = 1.75M,
                    Rating = 2.6M,
                    Stock = 20
                },
                new Product
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
            Assert.True(obj is IEnumerable<Product>);
            //Assert.Equal(inventory,obj);
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact]
        public async Task CheckoutValidCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var service2 = new CartService(context);
            var controller2 = new CGullProject.Controllers.CartController(service2);
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
            var result = await controller2.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task CheckoutInvalidCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var service2 = new CartService(context);
            var controller2 = new CGullProject.Controllers.CartController(service2);
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
            var result = await controller2.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkResult>(result);
        }
        [Fact]
        public async Task CheckoutExpiredCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var service2 = new CartService(context);
            var controller2 = new CGullProject.Controllers.CartController(service2);
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
            var result = await controller2.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkResult>(result);
        }

        [Fact]
        public async Task CheckoutInvalidCVVCreditCard()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var service2 = new CartService(context);
            var controller2 = new CGullProject.Controllers.CartController(service2);
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
            var result = await controller2.ProcessPayment(_cartId, cardNumber, theDate, cardHolderName, cvv);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkResult>(result);
        }

        [Fact]
        public async Task TotalsTestCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new ProductService(context);
            var revService = new ReviewService(context);
            var controller = new CGullProject.Controllers.ItemController(service, revService);
            var service2 = new CartService(context);
            var controller2 = new CGullProject.Controllers.CartController(service2);
            var cartId = await controller2.CreateNewCart("NewCart");
            OkObjectResult cart = Assert.IsType<OkObjectResult>(cartId);
            var cartID = (Guid)cart.Value;
            var itemId = "000001";
            var quantity = 1;
            var bundleId = "100020";

            var res1 = await controller.AddItemToCart(cartID, itemId, quantity);
            var res2 = await controller.AddItemToCart(cartID, bundleId, quantity);
            //Act 

            var result = await controller2.GetTotals(cartID);
            OkObjectResult objectResponse = Assert.IsType<OkObjectResult>(result);
            var obj = objectResponse.Value as TotalsDTO;

            //Assert
            Assert.NotNull(obj);
            Assert.IsType<TotalsDTO>(obj);
            Assert.Equal((decimal)1.75, obj.RegularTotal);
            Assert.Equal((decimal)7.81, obj.TotalWithTax);
            Assert.Equal((decimal)5.00, obj.BundleTotal);
            Assert.IsType<OkObjectResult>(res1);
            Assert.IsType<OkObjectResult>(res2);
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