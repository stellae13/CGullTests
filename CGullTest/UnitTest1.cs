using CGullProject;
using CGullProject.Data;
using CGullProject.Models;
using CGullProject.Models.DTO;
using CGullProject.Services;
using CGullProject.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace UnitTest
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
                        new Item
                        {
                            Id = "000001",
                            Name = "Seagull Drink",
                            CategoryId = 1,
                            MSRP = 1.75M,
                            SalePrice = 1.75M,
                            Rating = 2.6M,
                            Stock = 20,
                            IsBundle = false,
                            OnSale = false

                        },
                        new Item
                        {
                            Id = "000002",
                            Name = "Seagull Chips",
                            CategoryId = 1,
                            SalePrice = 5.99M,
                            MSRP = 5.99M,
                            Rating = 4.5M,
                            Stock = 25,
                            IsBundle = false,
                            OnSale = true
                        },
                        new Item
                        {
                            Id = "000003",
                            Name = "Seagull Ceral",
                            CategoryId = 1,
                            SalePrice = 5.99M,
                            MSRP = 5.99M,
                            Rating = 4.5M,
                            Stock = 25,
                            IsBundle = false,
                            OnSale = true
                        },
                        new Item
                        {
                            Id = "000004",
                            Name = "Seagull Ceral",
                            CategoryId = 1,
                            SalePrice = 5.99M,
                            MSRP = 5.99M,
                            Rating = 4.5M,
                            Stock = 25,
                            IsBundle = false,
                            OnSale = true
                        },
                        new Item
                        {
                            Id = "100020",
                            Name = "Food Bundle",
                            CategoryId = 1,
                            MSRP = 6.00M,
                            SalePrice = 5.00M,
                            Rating = 4.2M,
                            Stock = 20,
                            IsBundle = true,
                            OnSale = false
                        },
                        new Item
                        {
                            Id = "100021",
                            Name = "Food",
                            CategoryId = 1,
                            MSRP = 6.00M,
                            SalePrice = 5.00M,
                            Rating = 4.2M,
                            Stock = 20,
                            IsBundle = true,
                            OnSale = false
                        },
                        new Item
                        {
                            Id = "000021",
                            Name = "Food Bundle",
                            CategoryId = 1,
                            MSRP = 6.00M,
                            SalePrice = 5.00M,
                            Rating = 4.2M,
                            Stock = 20,
                            IsBundle = false,
                            OnSale = false
                        }); ; ;
                        //end of inventory 
                        context.Cart.AddRange(
                         new Cart()
                         {
                             Id = Guid.Parse("2c6dce67-a449-4eab-b4df-1641172fbd92"),
                             Name = "Stella"
                         }, new Cart()
                         {
                             Id = Guid.NewGuid(),
                             Name = "Test"
                         });
                        // end of cart
                        context.CartItem.AddRange(); // no items in cart at beginning of tests 
                        //end of CartItem
                        context.Bundle.AddRange(
                        new Bundle()
                        {
                            ItemId = "100020",
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now + TimeSpan.FromDays(100)
                        });
                        //end of Bundle
                        context.BundleItem.AddRange(
                            new BundleItem()
                            {
                                BundleId = "100020",
                                ItemId = "000001"
                            },
                            new BundleItem()
                            {
                                BundleId = "100020",
                                ItemId = "000002"
                            });
                        //end of bundle items
                        using (SHA256 shaCtx = SHA256.Create())
                        {
                            context.Admins.AddRange(
                            new Admins()
                            {
                                Username = "manager",
                                Password = shaCtx.ComputeHash(Encoding.UTF8.GetBytes("password"))

                            });
                        }
                        // end of admins
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
            var service = new ItemService(context);
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


            //Act
            var res = await controller.GetCart(cartId.First());
            ActionResult<CartDTO> objectResponse = Assert.IsAssignableFrom<ActionResult<CartDTO>>(res);
            var cart = objectResponse.Result;


            //Assert
            Assert.NotNull(res);
            Assert.NotNull(cart);
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
            var cartID = cartId.First();
            var itemId = "000003";
            var quantity = 25;

            //Act 
            var result = await controller.AddItemToCart(cartID, itemId, quantity);
            var item = from i in context.Inventory
                       where i.Id == itemId
                       select i;
            var cartIt = from c in context.CartItem
                         where c.CartId == cartID && c.ItemId == itemId
                         select c;

            var cartItem = cartIt.First().ItemId;
            var stock = item.First().Stock;

            //Assert
            Assert.NotNull(result);
            Assert.NotEmpty(cartIt);
            Assert.Equal("000003", cartItem);
            Assert.Equal(0, stock);
            Assert.IsType<OkObjectResult>(result);
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
            Assert.IsNotType<OkObjectResult>(result);

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
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
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
                         where c.CartId == cartID && c.ItemId == itemId
                         select c;
            var cartItem = cartIt.First().ItemId;
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
            var service = new InventoryService(context);
            var controller = new CGullProject.Controllers.InventoryController(service);

            //Act
            var res = await controller.GetAllItems();
            ActionResult<IEnumerable<Item>> objectResponse = Assert.IsAssignableFrom<ActionResult<IEnumerable<Item>>>(res);

            //Assert
            Assert.NotNull(objectResponse);
            Assert.IsType<OkObjectResult>(objectResponse.Result);
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
            ActionResult<ProcessPaymentDTO> objectResponse = Assert.IsAssignableFrom<ActionResult<ProcessPaymentDTO>>(result);
            var obj = objectResponse.Result;

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(obj);
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
            Assert.IsNotType<OkResult>(result);
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
            Assert.IsNotType<OkResult>(result);
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
            Assert.IsNotType<OkResult>(result);
        }

        [Fact]
        public async Task TotalsTestCart()
        {

            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cart = from c in context.Cart where c.Name == "Stella" select c.Id;
            Guid cartId = cart.First();
            await controller.AddItemToCart(cartId, "000001", 1);
            await controller.AddItemToCart(cartId, "100020", 1);

            //Act 
            var result = await controller.GetTotals(cartId);
            ActionResult<TotalsDTO> objectResponse = Assert.IsAssignableFrom<ActionResult<TotalsDTO>>(result);
            var obj = objectResponse.Value;

            //Assert
            Assert.NotNull(obj);
            Assert.IsType<TotalsDTO>(obj);
            Assert.Equal((decimal)1.75, obj.RegularTotal);
            Assert.Equal((decimal)8.81, obj.TotalWithTax);
            Assert.Equal((decimal)6.00, obj.BundleTotal);
        }

        //end of Project Phase 1 Tests 

        //beginning Project Phase 2 Tests

        [Fact]
        public async void AddItemToInventory()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new InventoryService(context);
            var controller = new CGullProject.Controllers.InventoryController(service);
            ItemDTO toAdd = new ItemDTO()
            {
                Id = "000704",
                Name = "Test",
                CategoryId = 1,
                SalePrice = 5.70M,
                MSRP = 5.99M,
                Rating = 4.5M,
                Stock = 25,
                IsBundle = false,
                //OnSale = true
            };

            //Act 
            var result = await controller.AddNewItem(toAdd); ;
            ActionResult<bool> objectResponse = Assert.IsAssignableFrom<ActionResult<bool>>(result);
            var obj = objectResponse.Result;


            //Assert
            Assert.NotNull(obj);
            Assert.IsAssignableFrom<OkObjectResult>(obj);
        }


        [Fact]
        public async void RemoveItemFromCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cartId = from b in context.Cart
                         where b.Name == "Stella"
                         select b.Id;
            var itemId = "000004";
            var quantity = 20;
            var res = await controller.AddItemToCart(cartId.First(), itemId, quantity);

            //Act 
            var result = await controller.RemoveFromCart(cartId.First(), itemId);


            //Assert
            Assert.IsType<OkObjectResult>(res);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async void RemoveItemFromCartNotInCart()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new CartService(context);
            var controller = new CGullProject.Controllers.CartController(service);
            var cart = await controller.CreateNewCart("New");
            Guid cartId = cart.Value;

            //Act 
            var result = await controller.RemoveFromCart(cartId, "");
            ObjectResult objectResponse = Assert.IsAssignableFrom<ObjectResult>(result);
            var obj = objectResponse.Value;

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(obj);
            Assert.IsNotType<OkObjectResult>(result);
        }

        [Fact]

        public async void GetAllSalesItems()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new InventoryService(context);
            var controller = new CGullProject.Controllers.InventoryController(service);

            //Act
            var res = await controller.GetAllSalesItems();
            ActionResult<IEnumerable<Item>> objectResponse = Assert.IsAssignableFrom<ActionResult<IEnumerable<Item>>>(res);

            //Assert
            Assert.NotNull(objectResponse);
            Assert.IsType<OkObjectResult>(objectResponse.Result);
        }

        [Fact]

        public async void ChangeSaleStatus()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new InventoryService(context);
            var controller = new CGullProject.Controllers.InventoryController(service);

            //Act
            var res = await controller.ChangeSaleStatus("000001", false);
            ActionResult<bool> objectResponse = Assert.IsAssignableFrom<ActionResult<bool>>(res);
            var obj = objectResponse.Result;

            //Assert
            Assert.NotNull(res);
            Assert.IsAssignableFrom<OkObjectResult>(obj);
        }

        [Fact]
        public async void LoginValid()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new AdminService(context);
            var controller = new CGullProject.Controllers.AdminsController(service);

            //Act 
            var result = await controller.Login("manager", "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8");
            ObjectResult objectResponse = Assert.IsAssignableFrom<ObjectResult>(result);
            var obj = objectResponse.Value;

            //Assert
            Assert.NotNull(result);
            Assert.Equal("Login successful", obj);
        }
        [Fact]
        public async void LoginInvalidUsername()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new AdminService(context);
            var controller = new CGullProject.Controllers.AdminsController(service);

            //Act 
            var result = await controller.Login("invalid", "password");
            ObjectResult objectResponse = Assert.IsAssignableFrom<ObjectResult>(result);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkObjectResult>(objectResponse);

        }

        [Fact]
        public async void LoginValidUsernameInvalidPassword()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new AdminService(context);
            var controller = new CGullProject.Controllers.AdminsController(service);

            //Act 
            var result = await controller.Login("stellagarcia", "passrd");
            ActionResult objectResponse = Assert.IsAssignableFrom<ActionResult>(result);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async void GetAllAdminsTest()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new AdminService(context);
            var controller = new CGullProject.Controllers.AdminsController(service);

            //Act 
            var result = await controller.GetAllAdmins(); ;
            OkObjectResult objectResponse = Assert.IsAssignableFrom<OkObjectResult>(result);
            var obj = objectResponse.Value;

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(obj);
            Assert.IsAssignableFrom<IEnumerable<Admins>>(obj);
        }
        [Fact]
        public async void AddAdmin()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new AdminService(context);
            var controller = new CGullProject.Controllers.AdminsController(service);

            //Act 
            var result = await controller.AddAdmin("manager", "newAdmin", "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8;5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"); ;
            ObjectResult objectResponse = Assert.IsAssignableFrom<ObjectResult>(result);
            var obj = objectResponse.Value;

            //Assert
            Assert.NotNull(result);
            Assert.Equal("User with username, \"newAdmin\" successfully created.", obj);


        }
        [Fact]
        public async void AddAdminInvalidCurrentAdmin()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new AdminService(context);
            var controller = new CGullProject.Controllers.AdminsController(service);

            //Act 
            var result = await controller.AddAdmin("stellrcia", "newAdmin", "password;password1"); ;
            ObjectResult objectResponse = Assert.IsAssignableFrom<ObjectResult>(result);
            var obj = objectResponse.Value;

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkObjectResult>(objectResponse);
            Assert.Equal("Failed to log in. No user added to system.", obj);

        }
        [Fact]
        public async void AddAdminUsernameAlreadyTaken()
        {
            //Arrange
            using var context = Fixture.CreateContext();
            var service = new AdminService(context);
            var controller = new CGullProject.Controllers.AdminsController(service);

            //Act 
            var result = await controller.AddAdmin("manager", "manager", "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8;5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"); ;
            ObjectResult objectResponse = Assert.IsAssignableFrom<ObjectResult>(result);
            var obj = objectResponse.Value;

            //Assert
            Assert.NotNull(result);
            Assert.IsNotType<OkObjectResult>(objectResponse);
            Assert.Equal("User with username, \"manager\" already exists", obj);

        }

    }
}
