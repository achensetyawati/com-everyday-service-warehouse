using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Com.Bateeq.Service.Warehouse.Lib;
using Com.Bateeq.Service.Warehouse.Lib.Facades;
using Com.Bateeq.Service.Warehouse.Lib.Helpers;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces;
using Com.Bateeq.Service.Warehouse.Lib.Models.InventoryModel;
using Com.Bateeq.Service.Warehouse.Lib.Services;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.SpkDocsViewModel;
using Com.Bateeq.Service.Warehouse.Test.DataUtils.InventoryDataUtils;
using Com.Bateeq.Service.Warehouse.Test.DataUtils.SPKDocDataUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace Com.Bateeq.Service.Warehouse.Test.Facades.SPKDocFacades
{
    public class SPKDocFacedeTests
    {
        private const string ENTITY = "MMInventory";

        private const string USERNAME = "Unit Test";
        private IServiceProvider ServiceProvider { get; set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            return string.Concat(sf.GetMethod().Name, "_", ENTITY);
        }
        
        private Mock<IServiceProvider> GetServiceProvider()
        {
            HttpResponseMessage message = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            message.Content = new StringContent("{\"apiVersion\":\"1.0\",\"statusCode\":200,\"message\":\"Ok\",\"data\":[{\"Id\":7,\"code\":\"USD\",\"rate\":13700.0,\"date\":\"2018/10/20\"}],\"info\":{\"count\":1,\"page\":1,\"size\":1,\"total\":2,\"order\":{\"date\":\"desc\"},\"select\":[\"Id\",\"code\",\"rate\",\"date\"]}}");
            HttpResponseMessage messagePost = new HttpResponseMessage();
            var HttpClientService = new Mock<IHttpClientService>();
            HttpClientService
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(message);
            HttpClientService
                .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(messagePost);
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IdentityService)))
                .Returns(new IdentityService() { Token = "Token", Username = "Test" });

            serviceProvider
                .Setup(x => x.GetService(typeof(IHttpClientService)))
                .Returns(HttpClientService.Object);

            return serviceProvider;
        }
        
        private WarehouseDbContext _dbContext(string testName)
        {
            DbContextOptionsBuilder<WarehouseDbContext> optionsBuilder = new DbContextOptionsBuilder<WarehouseDbContext>();
            optionsBuilder
                .UseInMemoryDatabase(testName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            WarehouseDbContext dbContext = new WarehouseDbContext(optionsBuilder.Options);

            return dbContext;
        }
        
        private InventoryDataUtil dataUtil(InventoryFacade facade, string testName, WarehouseDbContext dbContext)
        {
            var pkbbjfacade = new InventoryFacade(ServiceProvider, _dbContext(testName));
            //var sPKDocDataUtil = new SPKDocDataUtil(pkbbjfacade);
            //var transferFacade = new TransferFacade(ServiceProvider, _dbContext(testName));
            //var transferDataUtil = new TransferDataUtil(transferFacade, sPKDocDataUtil);

            return new InventoryDataUtil(facade, dbContext);
        }

        private SPKDocsFromFinihsingOutsViewModel ViewModel
        {
            get
            {
                return new SPKDocsFromFinihsingOutsViewModel
                {
                    FinishingOutDate = DateTimeOffset.Now,
                    UnitTo = new DestinationViewModel
                    {
                        _id = 1,
                        code = "code",
                        name = "name"
                    },
                    Unit = new SourceViewModel
                    {
                        code = "code",
                        name = "name",
                        _id = 1
                    },
                    PackingList = "0001/FER/08/21",
                    Password = "pass",
                    IsDifferentSize = false,
                    Weight = 0,
                    Comodity = new Comodity()
                    {
                        code = "code",
                        name = "name",
                        id = 1
                    },
                    RONo = "2110003",
                    Items = new List<SPKDocItemsFromFinihsingOutsViewModel>
                    {
                        new SPKDocItemsFromFinihsingOutsViewModel
                        {
                            Quantity = 20,
                            Size = new SizeObj()
                            {
                                Id = 1,
                                Size = "S"
                            },
                            Uom = new Uom()
                            {
                                Id = 1,
                                Unit = "PCS"
                            },
                            BasicPrice = 1000,
                            ComodityPrice = 10000,
                            IsDifferentSize = false,
                            Details = new List<Details>()
                            {
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 1,
                                        Size = "S"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                },
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 2,
                                        Size = "M"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                }
                            }
                        }
                    }
                };
            }
        }
        
        private SPKDocsFromFinihsingOutsViewModel ViewModelItemExist
        {
            get
            {
                return new SPKDocsFromFinihsingOutsViewModel
                {
                    FinishingOutDate = DateTimeOffset.Now,
                    UnitTo = new DestinationViewModel
                    {
                        _id = 1,
                        code = "code",
                        name = "name"
                    },
                    Unit = new SourceViewModel
                    {
                        code = "code",
                        name = "name",
                        _id = 1
                    },
                    PackingList = "0001/FER/08/21",
                    Password = "pass",
                    IsDifferentSize = false,
                    Weight = 0,
                    Comodity = new Comodity()
                    {
                        code = "code",
                        name = "name",
                        id = 1
                    },
                    RONo = "2110003",
                    Items = new List<SPKDocItemsFromFinihsingOutsViewModel>
                    {
                        new SPKDocItemsFromFinihsingOutsViewModel
                        {
                            Quantity = 20,
                            Size = new SizeObj()
                            {
                                Id = 12345,
                                Size = "S"
                            },
                            Uom = new Uom()
                            {
                                Id = 1,
                                Unit = "PCS"
                            },
                            BasicPrice = 1000,
                            ComodityPrice = 10000,
                            IsDifferentSize = false,
                            Details = new List<Details>()
                            {
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 1,
                                        Size = "S"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                },
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 2,
                                        Size = "M"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                }
                            }
                        }
                    }
                };
            }
        }
        
        private SPKDocsFromFinihsingOutsViewModel ViewModelDifferentSize
        {
            get
            {
                return new SPKDocsFromFinihsingOutsViewModel
                {
                    FinishingOutDate = DateTimeOffset.Now,
                    UnitTo = new DestinationViewModel
                    {
                        _id = 1,
                        code = "code",
                        name = "name"
                    },
                    Unit = new SourceViewModel
                    {
                        code = "code",
                        name = "name",
                        _id = 1
                    },
                    PackingList = "0001/FER/08/21",
                    Password = "pass",
                    IsDifferentSize = true,
                    Weight = 0,
                    Comodity = new Comodity()
                    {
                        code = "code",
                        name = "name",
                        id = 1
                    },
                    RONo = "2110003",
                    Items = new List<SPKDocItemsFromFinihsingOutsViewModel>
                    {
                        new SPKDocItemsFromFinihsingOutsViewModel
                        {
                            Quantity = 20,
                            Size = new SizeObj()
                            {
                                Id = 1,
                                Size = "S"
                            },
                            Uom = new Uom()
                            {
                                Id = 1,
                                Unit = "PCS"
                            },
                            BasicPrice = 1000,
                            ComodityPrice = 10000,
                            IsDifferentSize = true,
                            Details = new List<Details>()
                            {
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 1,
                                        Size = "S"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                },
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 2,
                                        Size = "M"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                }
                            }
                        }
                    }
                };
            }
        }
        
        private SPKDocsFromFinihsingOutsViewModel ViewModelDifferentSizeItemExist
        {
            get
            {
                return new SPKDocsFromFinihsingOutsViewModel
                {
                    FinishingOutDate = DateTimeOffset.Now,
                    UnitTo = new DestinationViewModel
                    {
                        _id = 1,
                        code = "code",
                        name = "name"
                    },
                    Unit = new SourceViewModel
                    {
                        code = "code",
                        name = "name",
                        _id = 1
                    },
                    PackingList = "0001/FER/08/21",
                    Password = "pass",
                    IsDifferentSize = true,
                    Weight = 0,
                    Comodity = new Comodity()
                    {
                        code = "code",
                        name = "name",
                        id = 1
                    },
                    RONo = "2110003",
                    Items = new List<SPKDocItemsFromFinihsingOutsViewModel>
                    {
                        new SPKDocItemsFromFinihsingOutsViewModel
                        {
                            Quantity = 20,
                            Size = new SizeObj()
                            {
                                Id = 12345,
                                Size = "S"
                            },
                            Uom = new Uom()
                            {
                                Id = 1,
                                Unit = "PCS"
                            },
                            BasicPrice = 1000,
                            ComodityPrice = 10000,
                            IsDifferentSize = true,
                            Details = new List<Details>()
                            {
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 1,
                                        Size = "S"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                },
                                new Details()
                                {
                                    ParentProduct = new Product()
                                    {
                                        Id = 1,
                                        Name = "Baju",
                                        Code = "1231"
                                    },
                                    Size = new SizeObj()
                                    {
                                        Id = 2,
                                        Size = "M"
                                    },
                                    Uom = new Uom()
                                    {
                                        Id = 1,
                                        Unit = "PCS"
                                    },
                                    Quantity = 10
                                }
                            }
                        }
                    }
                };
            }
        }
        
        [Fact]
        public async Task Should_Success_Create()
        {
            DbSet<Inventory> dbSetInventory = _dbContext(GetCurrentMethod()).Set<Inventory>();
            SPKDocsFacade facade = new SPKDocsFacade(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            
            //dbSetInventory.Add(model);
            //var Created = await _dbContext(GetCurrentMethod()).SaveChangesAsync();
            //InventoryFacade facade = new InventoryFacade(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            //var model = await dataUtil(facade, GetCurrentMethod()).GetTestData();
            var Response = await facade.Create(this.ViewModel, "username", "Bearer");
            Assert.NotEqual(0, Response);
        }
        
        [Fact]
        public async Task Should_Success_Create_Item_Exist()
        {
            DbSet<Inventory> dbSetInventory = _dbContext(GetCurrentMethod()).Set<Inventory>();
            SPKDocsFacade facade = new SPKDocsFacade(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            
            //dbSetInventory.Add(model);
            //var Created = await _dbContext(GetCurrentMethod()).SaveChangesAsync();
            //InventoryFacade facade = new InventoryFacade(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            //var model = await dataUtil(facade, GetCurrentMethod()).GetTestData();
            var Response = await facade.Create(this.ViewModelItemExist, "username", "Bearer");
            Assert.NotEqual(0, Response);
        }
        
        [Fact]
        public async Task Should_Success_Create_DifferentSize()
        {
            string itemUri = "items/finished-goods/Code";
            DbSet<Inventory> dbSetInventory = _dbContext(GetCurrentMethod()).Set<Inventory>();
            SPKDocsFacade facade = new SPKDocsFacade(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            
            var mockHttpClient = new Mock<IHttpClientService>();
            mockHttpClient.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = null
                });
            var Response = await facade.Create(this.ViewModelDifferentSize, "username", "Bearer");
            Assert.NotEqual(0, Response);
        }
        
        [Fact]
        public async Task Should_Success_Create_DifferentSize_Item_Exist()
        {
            string itemUri = "items/finished-goods/Code";
            DbSet<Inventory> dbSetInventory = _dbContext(GetCurrentMethod()).Set<Inventory>();
            SPKDocsFacade facade = new SPKDocsFacade(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            
            var mockHttpClient = new Mock<IHttpClientService>();
            mockHttpClient.Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = null
                });
            var Response = await facade.Create(this.ViewModelDifferentSizeItemExist, "username", "Bearer");
            Assert.NotEqual(0, Response);
        }
    }
}