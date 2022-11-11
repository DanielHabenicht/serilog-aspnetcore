using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Extensions.Hosting;

namespace Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Write streamlined request completion events, instead of the more verbose ones from the framework.
            // To use the default framework request logging instead, remove this line and set the "Microsoft"
            // level in appsettings.json to "Information".

            app.UseSerilogRequestLogging();
            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }

    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticContext _diagnosticContext;

        public RequestResponseLoggingMiddleware(RequestDelegate next, DiagnosticContext test, IDiagnosticContext test2)
        {
            _next = next;
            _diagnosticContext = test2;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var diagnosticContext = httpContext.RequestServices.GetService<IDiagnosticContext>();
            // Read and log request body data
            string requestBodyPayload = await ReadRequestBody(httpContext.Request);
            _diagnosticContext.Set("RequestBody", requestBodyPayload.Replace("\n", ""));
            _diagnosticContext.Set("Test", "Cool");
            diagnosticContext.Set("Test", "Winner");

            //// Read and log response body data
            //// Copy a pointer to the original response body stream
            //var originalResponseBodyStream = httpContext.Response.Body;

            //// Create a new memory stream...
            //using (var responseBody = new MemoryStream())
            //{
            //	// ...and use that for the temporary response body
            //	httpContext.Response.Body = responseBody;

            //	// Continue down the Middleware pipeline, eventually returning to this class
            //	await _next(httpContext);

            //	// Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
            //	await responseBody.CopyToAsync(originalResponseBodyStream);
            //}

            //string responseBodyPayload = await ReadResponseBody(httpContext.Response);
            //diagnosticContext.Set("ResponseBody", responseBodyPayload);
            await _next(httpContext);
            diagnosticContext.Set("Test", "Winner2");
            _diagnosticContext.Set("Test", "Cool2");

        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            HttpRequestRewindExtensions.EnableBuffering(request);

            var body = request.Body;
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            string requestBody = Encoding.UTF8.GetString(buffer);
            body.Seek(0, SeekOrigin.Begin);
            request.Body = body;

            return $"{requestBody}";
        }

        private static async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"{responseBody}";
        }

    }
}