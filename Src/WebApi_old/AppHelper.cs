namespace WebApi
{
    using WebApi.BackgroundServices;

    public class AppHelper
    {
        public static WebApplication BuildApp(string[] args)
        {

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Background services
            builder.Services.AddHostedService<StatisticsBackgroundService>();

            // IWebApiRegistrationHelper regHelper = WebApiRegistrationHelper.Instance;
            // regHelper.RegisterServices(builder.Services);

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
            return app;
        }
    }
}
