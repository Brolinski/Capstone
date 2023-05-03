using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scheduler.Domain.Models;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.Services;
using Scheduler.Infrastructure.Persistence;
using Scheduler.Infrastructure.Repositories;
using Scheduler.Options;
using Scheduler.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

// Configure database
#if DEBUG
const string CONN = "Local";
#else
const string CONN = "Hosted";
#endif

string connectionString = builder.Configuration.GetConnectionString(CONN)
	?? throw new ArgumentException("Could not retrieve connection string.");

builder.Services
	.AddDbContext<SchedulerContext>(o => o
		.UseSqlServer(connectionString)
		.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking))
	.AddScoped<IScheduleRepository, ScheduleRepository>()
	.AddScoped<IFieldRepository, FieldRepository>()
	.AddScoped<ILeagueRepository, LeagueRepository>()
	.AddScoped<ITeamRepository, TeamRepository>();

builder.Services
	.AddHostedService<ScheduleCullingService>()
	.AddSingleton<IDateProvider, SystemDateProvider>()
	.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services
	.AddIdentity<User, Role>()
	.AddEntityFrameworkStores<SchedulerContext>()
	.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.ExpireTimeSpan = TimeSpan.FromDays(1);
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
	options.TokenLifespan = TimeSpan.FromHours(2));

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

builder.Services.AddControllersWithViews();

builder.Services
	.AddOptions<CullingOptions>()
	.Bind(builder.Configuration.GetSection(CullingOptions.Culling))
	.ValidateDataAnnotations();

builder.Services
	.AddOptions<SmtpOptions>()
	.Bind(builder.Configuration.GetSection(SmtpOptions.Smtp))
	.ValidateDataAnnotations();

builder.Services
	.AddOptions<EmailOptions>()
	.Bind(builder.Configuration.GetSection(EmailOptions.Email))
	.ValidateDataAnnotations();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage()
	   .UseMigrationsEndPoint();
}
else if (app.Environment.IsProduction())
{
	app.UseExceptionHandler("/Error/500")
	   .UseStatusCodePagesWithRedirects("/Error/{0}")
	   .UseHsts();
}

app.UseHttpsRedirection()
   .UseStaticFiles()
   .UseRouting()
   .UseAuthentication()
   .UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();