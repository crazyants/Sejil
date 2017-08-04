using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Sejil.Configuration.Internal;
using Sejil.Models.Internal;
using Sejil.Routing.Internal;
using Serilog.Events;
using Xunit;

namespace Sejil.Test
{
    public class ApplicationBuilderExtensionsTests
    {
        [Fact]
        public async Task HttpGet_root_url_calls_controller_GetIndexAsync_method()
        {
            // Arrange
            var url = "/sejil";
            var target = url;
            var controllerMoq = new Mock<ISejilController>();
            var server = CreateServer(url, controllerMoq.Object);

            // Act
            await server.CreateClient().GetAsync(target);

            // Assert
            controllerMoq.Verify(p => p.GetIndexAsync(It.IsAny<HttpContext>()), Times.Once);
        }

        [Theory]
        [MemberData(nameof(HttpPost_events_url_calls_controller_GetEventsAsync_method_TestData))]
        public async Task HttpPost_events_url_calls_controller_GetEventsAsync_method(string queryString,
            string bodyContent, int expectedPageArg, DateTime? expectedstartingTsArg, string expectedQueryArg)
        {
            // Arrange
            var url = "/sejil";
            var target = $"{url}/events{queryString}";
            var controllerMoq = new Mock<ISejilController>();
            var server = CreateServer(url, controllerMoq.Object);
            var content = bodyContent == null
                ? null
                : new StringContent(bodyContent);

            // Act
            await server.CreateClient().PostAsync(target, content);

            // Assert
            controllerMoq.Verify(p => p.GetEventsAsync(It.IsAny<HttpContext>(),
                expectedPageArg, expectedstartingTsArg, expectedQueryArg), Times.Once);
        }

        [Fact]
        public async Task HttpPost_log_query_url_calls_controller_SaveQueryAsync_method()
        {
            // Arrange
            var url = "/sejil";
            var target = $"{url}/log-query";
            var controllerMoq = new Mock<ISejilController>();
            var server = CreateServer(url, controllerMoq.Object);
            var logQuery = new LogQuery
            {
                Name = "Test",
                Query = "Test"
            };
            var content = new StringContent(JsonConvert.SerializeObject(logQuery));

            // Act
            await server.CreateClient().PostAsync(target, content);

            // Assert
            controllerMoq.Verify(p => p.SaveQueryAsync(It.IsAny<HttpContext>(),
                It.Is<LogQuery>(v => v.Name == logQuery.Name && v.Query == logQuery.Query)), Times.Once);
        }

        [Fact]
        public async Task HttpGet_log_queries_url_calls_controller_GetQueriesAsync_method()
        {
            // Arrange
            var url = "/sejil";
            var target = $"{url}/log-queries";
            var controllerMoq = new Mock<ISejilController>();
            var server = CreateServer(url, controllerMoq.Object);

            // Act
            await server.CreateClient().GetAsync(target);

            // Assert
            controllerMoq.Verify(p => p.GetQueriesAsync(It.IsAny<HttpContext>()), Times.Once);
        }

        [Fact]
        public async Task HttpPost_min_log_level_url_calls_controller_SetMinimumLogLevelAsync_method()
        {
            // Arrange
            var url = "/sejil";
            var target = $"{url}/min-log-level";
            var targetMinLogLevel = "info";
            var controllerMoq = new Mock<ISejilController>();
            var server = CreateServer(url, controllerMoq.Object);
            var content = new StringContent(targetMinLogLevel);

            // Act
            await server.CreateClient().PostAsync(target, content);

            // Assert
            controllerMoq.Verify(p => p.SetMinimumLogLevelAsync(It.IsAny<HttpContext>(), targetMinLogLevel), Times.Once);
        }

        public static IEnumerable<object[]> HttpPost_events_url_calls_controller_GetEventsAsync_method_TestData()
        {
            yield return new object[]
            {
                "", null, 0, null, null
            };
            yield return new object[]
            {
                "?page=2", "my query", 2, null, "my query"
            };
            yield return new object[]
            {
                "?page=2&startingTs=2017-08-04%2006%3A07%3A44.100", "my query", 2, new DateTime(2017, 8, 4, 6, 7, 44, 100), "my query"
            };
        }

        private static TestServer CreateServer(string url, ISejilController controller)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseSejil();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ISejilSettings>(new SejilSettings(url, LogEventLevel.Debug));
                    services.AddSingleton<ISejilController>(controller);
                    services.AddRouting();
                });

            return new TestServer(builder);
        }
    }
}