using AutoMapper;
using Com.Bateeq.Service.Warehouse.Lib;
using Com.Bateeq.Service.Warehouse.Lib.Facades.Stores;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces.Stores.TransferStocksInterfaces;
using Com.Bateeq.Service.Warehouse.Lib.Models.TransferModel;
using Com.Bateeq.Service.Warehouse.Lib.Services;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.TransferViewModels;
using Com.Bateeq.Service.Warehouse.Test.Helpers;
using Com.Bateeq.Service.Warehouse.WebApi.Controllers.v1.Stores.TransferStockController;
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
using Xunit;

namespace Com.Bateeq.Service.Warehouse.Test.Controllers.Store.TransferStockControllerTest
{
	public class TransferStockControllerTest
	{
		private TransferOutDocViewModel ViewModel
		{
			get
			{
				return new TransferOutDocViewModel
				{
					code = "code",
					 
					items =
					{
						 
					}
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

		private TransferStockController GetController(Mock<TransferStockFacade> facadeM, Mock<IValidateService> validateM, Mock<IMapper> mapper)
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

			TransferStockController controller = new TransferStockController(servicePMock.Object, mapper.Object, facadeM.Object)
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
		public TransferStockViewModel GetTestData(WarehouseDbContext dbContext)
		{
			TransferOutDoc data = new TransferOutDoc();
			dbContext.TransferOutDocs.Add(data);
			dbContext.SaveChanges();
			TransferStockViewModel viewModel = new TransferStockViewModel();
			viewModel.code = data.Code;
			 
			return viewModel;
			 
		}

		[Fact]
		public void Should_Error_Get()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferStockViewModel>())).Verifiable();

			var mockFacade = new Mock<TransferStockFacade>();


			mockFacade.Setup(x => x.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
				.Returns(Tuple.Create(new List<TransferOutDoc>(), 0, new Dictionary<string, string>()));

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutDocViewModel>>(It.IsAny<List<TransferInDoc>>()))
				.Returns(new List<TransferOutDocViewModel> { ViewModel });

			TransferStockController controller = GetController(mockFacade, validateMock, mockMapper);
			var response = controller.Get();
			Assert.Equal((int)HttpStatusCode.InternalServerError , GetStatusCode(response));
		}

		[Fact]
		public void Should_OK_Get()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferStockViewModel>())).Verifiable();

			var mockFacade = new Mock<TransferStockFacade>();


			mockFacade.Setup(x => x.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
				.Returns(Tuple.Create(new List<TransferOutDoc>(), 0, new Dictionary<string, string>()));

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutDocViewModel>>(It.IsAny<List<TransferOutDoc>>()))
				.Returns(new List<TransferOutDocViewModel> { ViewModel });

			TransferStockController controller = GetController(mockFacade, validateMock, mockMapper);
			var response = controller.Get();
			Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
		}

		[Fact]
		public void Should_Error_GetById()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferStockViewModel>())).Verifiable();

			var mockFacade = new Mock<TransferStockFacade>();


			mockFacade.Setup(x => x.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
				.Returns(Tuple.Create(new List<TransferOutDoc>(), 0, new Dictionary<string, string>()));

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutDocViewModel>>(It.IsAny<List<TransferOutDoc>>()))
				.Returns(new List<TransferOutDocViewModel> { ViewModel });


			TransferStockController controller = GetController(mockFacade, validateMock, mockMapper);
			var response = controller.Get(1);
			Assert.Equal((int)HttpStatusCode.InternalServerError, GetStatusCode(response));
		}
		[Fact]
		public void Should_POST_Get()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferStockViewModel>())).Verifiable();

			var mockFacade = new Mock<TransferStockFacade>();


			mockFacade.Setup(x => x.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
				.Returns(Tuple.Create(new List<TransferOutDoc>(), 0, new Dictionary<string, string>()));

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutDocViewModel>>(It.IsAny<List<TransferOutDoc>>()))
				.Returns(new List<TransferOutDocViewModel> { ViewModel });

			TransferStockController controller = GetController(mockFacade, validateMock, mockMapper);
		 
			IActionResult response =   controller.Post(ViewModel).Result;
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

			var mockFacade = new Mock<TransferStockFacade>();

			var mockMapper = new Mock<IMapper>();
			TransferStockController controller = GetController(mockFacade, validateMock, mockMapper);

			IActionResult response = controller.Post(null).Result;

			//Assert
			int statusCode = this.GetStatusCode(response);
			Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
		}

		[Fact]
		public async void GetPending_Return_OK()
		{
			var validateMock = new Mock<IValidateService>();
			validateMock.Setup(s => s.Validate(It.IsAny<TransferStockViewModel>())).Verifiable();

			var mockFacade = new Mock<TransferStockFacade>();


			mockFacade.Setup(x => x.ReadModel(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<string>()))
				.Returns(Tuple.Create(new List<TransferStockViewModel>(), 0, new Dictionary<string, string>()));

			var mockMapper = new Mock<IMapper>();
			mockMapper.Setup(x => x.Map<List<TransferOutDocViewModel>>(It.IsAny<List<TransferOutDoc>>()))
				.Returns(new List<TransferOutDocViewModel> { ViewModel });

			TransferStockController controller = GetController(mockFacade, validateMock, mockMapper);
			var response = controller.GetPending();
			Assert.Equal((int)HttpStatusCode.OK, GetStatusCode(response));
		}

	}
}
