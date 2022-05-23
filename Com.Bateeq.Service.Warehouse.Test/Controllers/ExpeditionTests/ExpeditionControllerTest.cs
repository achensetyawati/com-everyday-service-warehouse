using AutoMapper;
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
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
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
                    Code = "Code",
                    ExpeditionServiceCode = "ExpserviceCode"

                };

            }
        }

        private ServiceValidationExeption GetServiceValidationExeption()
        {
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            List<ValidationResult> validationResults = new List<ValidationResult>();
            System.ComponentModel.DataAnnotations.ValidationContext validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(expviewModel, serviceProvider.Object, null);
            return new ServiceValidationExeption(validationContext, validationResults);
        }

        protected int GetStatusCode(IActionResult response)
        {
            return (int)response.GetType().GetProperty("StatusCode").GetValue(response, null);
        }

        private ExpeditionController GetController(Mock<ExpeditionFacade> facadeM, Mock<IValidateService> validateM, Mock<IMapper> mapper)
        {
            var user = new Mock<ClaimsPrincipal>();
            var claims = new Claim[]
            {
                new Claim("username", "unittestusername")
            };
            user.Setup(u => u.Claims).Returns(claims);

            var servicePMock = GetServiceProvider();
            if (validateM != null)
            {
                servicePMock
                    .Setup(x => x.GetService(typeof(IValidateService)))
                    .Returns(validateM.Object);
            }

            ExpeditionController controller = new ExpeditionController(mapper.Object, facadeM.Object, servicePMock.Object)
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
        private Mock<IServiceProvider> GetServiceProvider()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IdentityService)))
                .Returns(new IdentityService() { Token = "Token", Username = "Test" });

            serviceProvider
                .Setup(x => x.GetService(typeof(IHttpClientService)))
                .Returns(new HttpClientTestService());

            return serviceProvider;
        }

        [Fact]
        public void Should_Success_Get_All_Data()
        {
            var validateMock = new Mock<IValidateService>();
            validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Verifiable();

            var mockFacade = new Mock<ExpeditionFacade>();


            mockFacade.Setup(x => x.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>() ))
                .Returns(Tuple.Create(new List<Expedition>(), 0, new Dictionary<string, string>()));

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<List<ExpeditionViewModel>>(It.IsAny<List<Expedition>>()))
                .Returns(new List<ExpeditionViewModel> { expviewModel });

            ExpeditionController controller = GetController(mockFacade, validateMock, mockMapper);
            var response = controller.Get();
            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }
        [Fact]
        public void Should_Error_Get_All_Data()
        {
            var validateMock = new Mock<IValidateService>();
            validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Verifiable();

            var mockFacade = new Mock<ExpeditionFacade>();


            mockFacade.Setup(x => x.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
                .Returns(Tuple.Create(new List<Expedition>(), 0, new Dictionary<string, string>()));

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<List<ExpeditionViewModel>>(It.IsAny<List<Expedition>>()))
                .Returns(new List<ExpeditionViewModel> { expviewModel });

            ExpeditionController controller = new ExpeditionController(mockMapper.Object, mockFacade.Object, GetServiceProvider().Object);
            var response = controller.Get();
            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }
        [Fact]
        public async Task Should_Success_Create_Data()
        {
            var validateMock = new Mock<IValidateService>();
            validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Verifiable();

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<Expedition>(It.IsAny<ExpeditionViewModel>()))
                .Returns(Model);

            var mockFacade = new Mock<ExpeditionFacade>();
            mockFacade.Setup(x => x.Create(It.IsAny<Expedition>(), "unittestusername", 7))
               .ReturnsAsync(1);

            var controller = GetController(mockFacade, validateMock, mockMapper);

            var response = await controller.Post(this.expviewModel);
            Assert.Equal((int)HttpStatusCode.Created, GetStatusCode(response));
        }
        [Fact]
        public async Task Should_Validate_Create_Data()
        {
            var validateMock = new Mock<IValidateService>();
            validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Throws(GetServiceValidationExeption());

            var mockMapper = new Mock<IMapper>();

            var mockFacade = new Mock<ExpeditionFacade>();

            var controller = GetController(mockFacade, validateMock, mockMapper);

            var response = await controller.Post(this.expviewModel);
            Assert.Equal((int)HttpStatusCode.BadRequest, GetStatusCode(response));
        }
        [Fact]
        public async Task Should_Error_Create_Data()
        {
            var validateMock = new Mock<IValidateService>();
            validateMock.Setup(s => s.Validate(It.IsAny<ExpeditionViewModel>())).Verifiable();

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<Expedition>(It.IsAny<ExpeditionViewModel>()))
                .Returns(Model);

            var mockFacade = new Mock<ExpeditionFacade>();
            mockFacade.Setup(x => x.Create(It.IsAny<Expedition>(), "unittestusername", 7))
               .ReturnsAsync(1);

            var controller = new ExpeditionController(mockMapper.Object, mockFacade.Object, GetServiceProvider().Object);

            var response = await controller.Post(this.expviewModel);
            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }
        [Fact]
        public void Should_Success_Get_Data_By_Id()
        {
            var mockFacade = new Mock<ExpeditionFacade>();

            mockFacade.Setup(x => x.ReadById(It.IsAny<int>()))
                .Returns(new Expedition());

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(x => x.Map<ExpeditionViewModel>(It.IsAny<Expedition>()))
                .Returns(expviewModel);

            ExpeditionController controller = new ExpeditionController(mockMapper.Object, mockFacade.Object, GetServiceProvider().Object);
            var response = controller.Get(It.IsAny<int>());
            Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
        }
        [Fact]
        public void Should_Error_Get_Data_By_Id()
        {
            var mockFacade = new Mock<ExpeditionFacade>();

            mockFacade.Setup(x => x.ReadById(It.IsAny<int>()))
                .Returns(new Expedition());

            var mockMapper = new Mock<IMapper>();

            ExpeditionController controller = new ExpeditionController(mockMapper.Object, mockFacade.Object, GetServiceProvider().Object);
            var response = controller.Get(It.IsAny<int>());
            Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
        }

    }

    
}
