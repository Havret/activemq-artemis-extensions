using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ActiveMQ.Artemis.Client.Examples.AspNetCore
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddActiveMq()
                    .AddConsumer("a1", RoutingType.Multicast, "q1", async (message, consumer, serviceProvider) =>
                    {
                        Console.WriteLine("q1: " + message.GetBody<string>());
                        await consumer.AcceptAsync(message);
                    })
                    .AddConsumer("a1", RoutingType.Multicast, "q2", async (message, consumer, serviceProvider) =>
                    {
                        Console.WriteLine("q2: " + message.GetBody<string>());
                        await consumer.AcceptAsync(message);
                    })
                    .AddProducer<MyTypedMessageProducer>("a1", RoutingType.Multicast);
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
                endpoints.MapGet("/", async context =>
                {
                    var messageProducer = context.RequestServices.GetService<MyTypedMessageProducer>();
                    await messageProducer.SendTextAsync(DateTime.UtcNow.ToLongDateString());
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}