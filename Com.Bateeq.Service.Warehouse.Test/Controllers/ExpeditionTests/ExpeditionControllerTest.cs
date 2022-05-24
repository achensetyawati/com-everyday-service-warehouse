using AutoMapper;
using Com.Bateeq.Service.Warehouse.Lib;
using Com.Bateeq.Service.Warehouse.Lib.Facades;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces;
using Com.Bateeq.Service.Warehouse.Lib.Models.Expeditions;
using Com.Bateeq.Service.Warehouse.Lib.Services;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.ExpeditionViewModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.TransferViewModels;
using Com.Bateeq.Service.Warehouse.Test.Helpers;
using Com.Bateeq.Service.Warehouse.WebApi.Controllers.v1.ExpeditionControllers;
using Com.Moonlay.NetCore.Lib.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Com.Bateeq.Service.Warehouse.Test.Controllers.ExpeditionTests
{
    public class ExpeditionControllerTest
    {
        private TransferOutDocViewModel viewModel
        {
            get
            {
                return new TransferOutDocViewModel
                {
                    code = "code",
                    expeditionService = new Lib.ViewModels.NewIntegrationViewModel.ExpeditionServiceViewModel
                    {
                        code = "codeexp"
                    },
                    remark = "remark",
                    source = new Lib.ViewModels.NewIntegrationViewModel.SourceViewModel
                    {
                        code = "codesource"
                    },
                    items = new List<TransferOutDocItemViewModel>
                    {
                        new TransferOutDocItemViewModel
                        {
                            item = new Lib.ViewModels.NewIntegrationViewModel.ItemViewModel
                            {
                                code = "codeitem"
                            },
                            quantity = 1,
                            remark = "remark item"
                        }
                    }
                };
            }
        }
        private ExpeditionViewModel expviewModel
        {
            get
            {
                return new ExpeditionViewModel
                {
                    Id = 1,
                    code = "code",
                    expeditionService = new Lib.ViewModels.NewIntegrationViewModel.ExpeditionServiceViewModel
                    {
                        code = "codeexp"
                    },
                    remark = "remark",
                    //source = new Lib.ViewModels.NewIntegrationViewModel.SourceViewModel
                    //{
                    //    code = "codesource"
                    //},
                    items = new List<ExpeditionItemViewModel>
                    {
                        new ExpeditionItemViewModel
                        {
                            spkDocsViewModel = new Lib.ViewModels.SpkDocsViewModel.SPKDocsViewModel
                            {
                                code = "code",
                                destination = new Lib.ViewModels.NewIntegrationViewModel.DestinationViewModel
                                {
                                    code = "code",
                                    name = "name",
                                    _id = 1
                                }
                            },
                            details = new List<ExpeditionDetailViewModel> {
                                new ExpeditionDetailViewModel
                                {
                                    item = new Lib.ViewModels.NewIntegrationViewModel.ItemViewModel
                                    {
                                        code = "code",
                                        name = "name"
                                    }
                                }
                            },
                            weight = 2
                        }

                    }
                };
            }
        }
        private Expedition Model
        {
            get
            {
                return new Expedition
                {
                    Id = 1,
                    Code = "Code",
                    ExpeditionServiceCode = "ExpserviceCode"

                };

            }
        }

        private Expedition ModelTest
        {
            get
            {
                return new Expedition
                {
                    Id = 1,
                    Code = "code",
                    Items = new List<ExpeditionItem>
                    {
                        new ExpeditionItem
                        {
                            Details = new List<ExpeditionDetail> {
                                new ExpeditionDetail
                                {
                                    ItemCode = "code"
                                }
                            }
                        }

                    }
                };
            }
        }
        //private IServiceProvider service;
        //private WarehouseDbContext dbContext;

        private ServiceValidationExeption GetServiceValidationExeption()
        {
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            List<ValidationResult> validationResults = new List<ValidationResult>();
            System.ComponentModel.DataAnnotations.ValidationContext validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(expviewModel, serviceProvider.Object, null);
            return new ServiceValidationExeption(validationContext, validationResults);
        }

        private ExpeditionController GetController(ExpeditionFacade facadeM, Mock<IMapper> mapper)
        {
            var user = new Mock<ClaimsPrincipal>();
            var claims = new Claim[]
            {
                new Claim("username", "unittestusername")
            };
            user.Setup(u => u.Claims).Returns(claims);

            var servicePMock = GetServiceProvider();

            ExpeditionController controller = new ExpeditionController(mapper.Object, facadeM, servicePMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        User = user.Object
                    }
                }
            };
            controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = "Bearer unittesttoken";
            controller.ControllerContext.HttpContext.Request.Path = new PathString("/v1/unit-test");
            controller.ControllerContext.HttpContext.Request.Headers["x-timezone-offset"] = "7";

            return controller;
        }

        private WarehouseDbContext _dbContext(string testName)
        {
            var serviceProvider = new ServiceCollection()
              .AddEntityFrameworkInMemoryDatabase()
              .BuildServiceProvider();

            DbContextOptionsBuilder<WarehouseDbContext> optionsBuilder = new DbContextOptionsBuilder<WarehouseDbContext>();
            optionsBuilder
                .UseInMemoryDatabase(testName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .UseInternalServiceProvider(serviceProvider);

            WarehouseDbContext dbContext = new WarehouseDbContext(optionsBuilder.Options);

            return dbContext;
        }

        protected string GetCurrentAsyncMethod([CallerMemberName] string methodName = "")
        {
            var method = new StackTrace()
                .GetFrames()
                .Select(frame => frame.GetMethod())
                .FirstOrDefault(item => item.Name == methodName);

            return method.Name;

        }

        public Expedition GetTestData(WarehouseDbContext dbContext)
        {
            //Expedition data = new Expedition();
            dbContext.Expeditions.Add(ModelTest);
            dbContext.SaveChanges();

            return ModelTest;
        }

        protected int GetStatusCode(IActionResult response)
        {
            return (int)response.GetType().GetProperty("StatusCode").GetValue(response, null);
        }

        private Mock<IServiceProvider> GetServiceProvider()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IdentityService)))
                .Returns(new IdentityService() { Token = "Token", Username = "Test" });

            var validateService = new Mock<IValidateService>();
            serviceProvider
              .Setup(s => s.GetService(typeof(IValidateService)))
              .Returns(validateService.Object);

            serviceProvider
                .Setup(x => x.GetService(typeof(IHttpClientService)))
                .Returns(new HttpClientTestService());

            return serviceProvider;
        }

        [Fact]
        public void Should_Success_Get_All_Data()
        {
            WarehouseDbContext dbContext = _dbContext(GetCurrentAsyncMethod());
            Mock<IServiceProvider> serviceProvider = GetServiceProvider();

            ExpeditionFacade facade = new ExpeditionFacade(serviceProvider.Object, dbContext);

            serviceProvider.Setup(s => s.GetService(typeof(ExpeditionFacade))).Returns(facade);
            serviceProvider.Setup(s => s.GetService(typeof(WarehouseDbContext))).Returns(dbContext);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<List<ExpeditionViewModel>>(It.IsAny<List<Expedition>>()))
                .Returns(new List<ExpeditionViewModel> { expviewModel });

            IActionResult response = GetController(facade, mockMapper).Get();

            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }

        [Fact]
        public void Should_Error_Get_All_Data()
        {

            WarehouseDbContext dbContext = _dbContext(GetCurrentAsyncMethod());
            Mock<IServiceProvider> serviceProvider = GetServiceProvider();

            ExpeditionFacade facade = new ExpeditionFacade(serviceProvider.Object, dbContext);

            serviceProvider.Setup(s => s.GetService(typeof(ExpeditionFacade))).Returns(facade);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<List<ExpeditionViewModel>>(It.IsAny<List<Expedition>>()));

            IActionResult response = GetController(facade, mockMapper).Get();

            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }

        [Fact]
        public void Should_Success_Get_Data_By_Id()
        {
            WarehouseDbContext dbContext = _dbContext(GetCurrentAsyncMethod());
            Mock<IServiceProvider> serviceProvider = GetServiceProvider();

            ExpeditionFacade facade = new ExpeditionFacade(serviceProvider.Object, dbContext);

            serviceProvider.Setup(s => s.GetService(typeof(ExpeditionFacade))).Returns(facade);
            serviceProvider.Setup(s => s.GetService(typeof(WarehouseDbContext))).Returns(dbContext);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<ExpeditionViewModel>(It.IsAny<Expedition>()))
                .Returns(expviewModel);

            Expedition testData = GetTestData(dbContext);

            //IActionResult response = GetController(facade, mockMapper).Get((int)testData.Id);
            IActionResult response = GetController(facade, mockMapper).Get(1);

            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }

        [Fact]
        public void Should_Error_Get_Data_By_Id()
        {
            WarehouseDbContext dbContext = _dbContext(GetCurrentAsyncMethod());
            Mock<IServiceProvider> serviceProvider = GetServiceProvider();

            ExpeditionFacade facade = new ExpeditionFacade(serviceProvider.Object, dbContext);

            serviceProvider.Setup(s => s.GetService(typeof(ExpeditionFacade))).Returns(facade);
            serviceProvider.Setup(s => s.GetService(typeof(WarehouseDbContext))).Returns(dbContext);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<List<ExpeditionViewModel>>(It.IsAny<List<Expedition>>()))
                .Returns(new List<ExpeditionViewModel> { expviewModel });

            Expedition testData = GetTestData(dbContext);

            IActionResult response = GetController(facade, mockMapper).Get(It.IsAny<int>());

            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }

        [Fact]
        public void Should_Success_Get_PDF()
        {
            WarehouseDbContext dbContext = _dbContext(GetCurrentAsyncMethod());
            Mock<IServiceProvider> serviceProvider = GetServiceProvider();

            ExpeditionFacade facade = new ExpeditionFacade(serviceProvider.Object, dbContext);

            serviceProvider.Setup(s => s.GetService(typeof(ExpeditionFacade))).Returns(facade);
            serviceProvider.Setup(s => s.GetService(typeof(WarehouseDbContext))).Returns(dbContext);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<ExpeditionViewModel>(It.IsAny<Expedition>()))
                .Returns(expviewModel);

            Expedition testData = GetTestData(dbContext);

            ExpeditionController controller = GetController(facade, mockMapper);
            controller.ControllerContext.HttpContext.Request.Headers["Accept"] = "application/pdf";

            //IActionResult response = GetController(facade, mockMapper).Get((int)testData.Id);
            IActionResult response = controller.GetExpeditionPDF(1);

            Assert.NotNull(response.GetType().GetProperty("FileStream"));
        }

        //[Fact]
        //public async Task Should_Success_Create_Data()
        //{
        //    var validateMock = new Mock<IValidateService>();
        //    validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Verifiable();

        //    var mockMapper = new Mock<IMapper>();
        //    mockMapper.Setup(x => x.Map<Expedition>(It.IsAny<ExpeditionViewModel>()))
        //        .Returns(Model);

        //    var mockFacade = new Mock<ExpeditionFacade>();
        //    mockFacade.Setup(x => x.Create(It.IsAny<Expedition>(), "unittestusername", 7))
        //       .ReturnsAsync(1);

        //    var controller = GetController(mockFacade, validateMock, mockMapper);

        //    var response = await controller.Post(this.expviewModel);
        //    Assert.Equal((int)HttpStatusCode.Created, GetStatusCode(response));
        //}

        //public async Task Should_Error_Create_Data()
        //{
        //    var validateMock = new Mock<IValidateService>();
        //    validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Verifiable();

        //    var mockMapper = new Mock<IMapper>();
        //    mockMapper.Setup(x => x.Map<Expedition>(It.IsAny<ExpeditionViewModel>()))
        //        .Returns(Model);

        //    var mockFacade = new Mock<ExpeditionFacade>();
        //    mockFacade.Setup(x => x.Create(It.IsAny<Expedition>(), "unittestusername", 7))
        //       .ReturnsAsync(1);

        //    var controller = new ExpeditionController(mockMapper.Object, mockFacade.Object, GetServiceProvider().Object);

        //    var response = await controller.Post(this.expviewModel);
        //    Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        //}

        //[Fact]
        //public async Task Should_Validate_Create_Data()
        //{
        //    var validateMock = new Mock<IValidateService>();
        //    validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Throws(GetServiceValidationExeption());

        //    var mockMapper = new Mock<IMapper>();

        //    var mockFacade = new Mock<ExpeditionFacade>();

        //    var controller = GetController(mockFacade, validateMock, mockMapper);

        //    var response = await controller.Post(this.expviewModel);
        //    Assert.Equal((int)HttpStatusCode.BadRequest, GetStatusCode(response));
        //}

        //[Fact]


    }


}
