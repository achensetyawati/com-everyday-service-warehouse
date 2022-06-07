using AutoMapper;
using Com.Bateeq.Service.Warehouse.Lib;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces.Stores.ReturnToCenterInterfaces;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces.Stores.TransferStocksInterfaces;
using Com.Bateeq.Service.Warehouse.Lib.Models.TransferModel;
using Com.Bateeq.Service.Warehouse.Lib.Services;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.TransferViewModels;
using Com.Bateeq.Service.Warehouse.Test.Helpers;
using Com.Bateeq.Service.Warehouse.WebApi.Controllers.v1.Stores.ReturnToCenterController;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Com.Bateeq.Service.Warehouse.Test.Controllers.Store.ReturnToCenterTests
{
	public class ReturnToCenterControllerTest
	{
		private TransferOutDocViewModel ViewModel
		{
			get
			{
				return new TransferOutDocViewModel
				{
					code = "code",
					reference = ""
				};
			}
		}
		private TransferOutReadViewModel readViewModel
		{
			get
			{
				return new TransferOutReadViewModel
				{
					code = "code"
				};
			}
		}
		private ServiceValidationExeption GetServiceValidationExeption()
		{
			Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
			List<ValidationResult> validationResults = new List<ValidationResult>();
			System.ComponentModel.DataAnnotations.ValidationContext validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(ViewModel, serviceProvider.Object, null);
			return new ServiceValidationExeption(validationContext, validationResults);
		}

		protected int GetStatusCode(IActionResult response)
		{
			return (int)response.GetType().GetProperty("StatusCode").GetValue(response, null);
		}

		private ReturnToCenterController GetController(Mock<IReturnToCenter> facadeM, Mock<IValidateService> validateM, Mock<IMapper> mapper)
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

			ReturnToCenterController controller = new ReturnToCenterController(servicePMock.Object, mapper.Object, facadeM.Object)
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
		public TransferOutReadViewModel GetTestData(WarehouseDbContext dbContext)
		{
			TransferOutDoc data = new TransferOutDoc();
			dbContext.TransferOutDocs.Add(data);
			dbContext.SaveChanges();
			TransferOutReadViewModel viewModel = new TransferOutReadViewModel();
			viewModel.code = data.Code;

			return viewModel;

		}

		[Fact]
		public void Should_Error_Get()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferOutReadViewModel>())).Verifiable();

			var mockFacade = new Mock<IReturnToCenter>();

			mockFacade.Setup(x => x.ReadForRetur(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
				.Returns(Tuple.Create(new List<TransferOutReadViewModel>(), 0, new Dictionary<string, string>()));

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutReadViewModel>>(It.IsAny<List<TransferOutReadViewModel>>()))
				.Returns(new List<TransferOutReadViewModel> { readViewModel });

			ReturnToCenterController controller = GetController(mockFacade, validateMock, mockMapper);
			var response = controller.GetRetur(1, 25, "", "", "");
			Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
		}

		[Fact]
		public void Should_Error_GetById()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferOutReadViewModel>())).Verifiable();

			var mockFacade = new Mock<IReturnToCenter>();

			mockFacade.Setup(x => x.ReadById(It.IsAny<int>()))
				.Returns(new TransferOutDoc());

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutDocViewModel>>(It.IsAny<List<TransferOutDocViewModel>>()))
				.Returns(new List<TransferOutDocViewModel> { ViewModel });


			ReturnToCenterController controller = GetController(mockFacade, validateMock, mockMapper);
			var response = controller.Get(1);
			Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
		}
		[Fact]
		public void Should_POST_OK()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferOutDocViewModel>())).Verifiable();

			var mockFacade = new Mock<IReturnToCenter>();


			mockFacade.Setup(x => x.ReadForRetur(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
				.Returns(Tuple.Create(new List<TransferOutReadViewModel>(), 0, new Dictionary<string, string>()));

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutDocViewModel>>(It.IsAny<List<TransferOutDoc>>()))
				.Returns(new List<TransferOutDocViewModel> { ViewModel });

			ReturnToCenterController controller = GetController(mockFacade, validateMock, mockMapper);

			IActionResult response = controller.Post(ViewModel).Result;
			//Assert
			int statusCode = this.GetStatusCode(response);
			Assert.NotEqual((int)HttpStatusCode.NotFound, statusCode);

		}
		[Fact]
		public void POST_InternalServerError()
		{
			//Setup
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(null)).Throws(new Exception());

			var mockFacade = new Mock<IReturnToCenter>();

			var mockMapper = new Mock<IMapper>();
			ReturnToCenterController controller = GetController(mockFacade, validateMock, mockMapper);

			IActionResult response = controller.Post(null).Result;

			//Assert
			int statusCode = this.GetStatusCode(response);
			Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
		}

		[Fact]
		public void getExcel()
		{
			var validateMock = new Mock<IValidateService>();
			var mockFacade = new Mock<IReturnToCenter>();
			mockFacade.Setup(x => x.GenerateExcel(It.IsAny<int>()))
				.Returns(new MemoryStream());
			var mockMapper = new Mock<IMapper>(); 
			//var INVFacade = new Mock<IGarmentInvoice>();
			var user = new Mock<ClaimsPrincipal>();
			var claims = new Claim[]
			{
				new Claim("username", "unittestusername")
			};
			user.Setup(u => u.Claims).Returns(claims);
			ReturnToCenterController controller = GetController(mockFacade, validateMock, mockMapper);
			controller.ControllerContext = new ControllerContext()
			{
				HttpContext = new DefaultHttpContext()
				{
					User = user.Object
				}
			};

			controller.ControllerContext.HttpContext.Request.Headers["x-timezone-offset"] = "0";
			var response = controller.GetXls(It.IsAny<int>());
			Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.GetType().GetProperty("ContentType").GetValue(response, null));
		}
		[Fact]
		public void Should_Error_Get_Excel()
		{
			var validateMock = new Mock<IValidateService>();
			var mockFacade = new Mock<IReturnToCenter>();
			var mockMapper = new Mock<IMapper>(); 

			ReturnToCenterController controller = GetController(mockFacade, validateMock, mockMapper);
			var response = controller.GetXls(It.IsAny<int>());
			Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
		}

	}
}
