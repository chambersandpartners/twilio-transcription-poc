using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TwilioMediaStreams.Models;
using TwilioMediaStreams.Services;
using WebSocketManager;

namespace TwilioMediaStreams
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ProjectSettings>(Configuration.GetSection("ProjectSettings"));
            services.AddControllers();
            services.AddWebSocketManager();
            
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;

            
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseWebSockets();
            app.MapWebSocketManager("/ws", serviceProvider.GetService<MediaStreamHandler>());
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
