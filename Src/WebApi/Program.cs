using Microsoft.Extensions.DependencyInjection.Extensions;
using WebApi.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

var inst = RestApi.Common.WebApiRegistrationHelper.Instance;

inst.RegisterServices(builder.Services);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Background services
// builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStatisticsBackgroundService, StatisticsBackgroundService>());
if (inst.DoRegisterHostedServices)
{
    builder.Services.AddHostedService<StatisticsBackgroundService>();
}
// builder.Services.AddHostedService<BackgroundService>(provider =>
// {
//    return new StatisticsBackgroundService(provider.GetRequiredService<ILogger<StatisticsBackgroundService>>());
// });


builder.Services.TryAddSingleton<IStatisticsService, StatisticsService>();

var app = builder.Build();

inst.RegisterServiceProvider(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

//Make Program public
public partial class Program
{
}
