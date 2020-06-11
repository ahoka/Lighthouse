using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Lighthouse.Services;
using Lighthouse.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lighthouse
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddControllers();

            services.AddHostedService<NodeBackgroundService>();
            
            services.AddSingleton<Cluster>();
            services.AddSingleton<RaftNodePersistence>();

            services.Configure<RaftConfiguration>(Configuration.GetSection("Raft"));
            services.Configure<PersistenceConfiguration>(Configuration.GetSection("Persistence"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<RaftService>();
                endpoints.MapGrpcService<MembershipService>();

                endpoints.MapControllers();
            });
        }
    }
}
