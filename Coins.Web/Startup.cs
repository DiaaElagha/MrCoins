using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Coins.Data.Data;
using Coins.Entities.Domins.Auth;
using Coins.Data.Repositories;
using Coins.Core.Services;
using Coins.Core;
using Coins.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using System.IO;
using Coins.Core.Settings;
using Coins.Services;
using Coins.Core.Services.ThiedParty;
using Coins.Data.Repositories.ThiedParty;

namespace Coins.Web
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(option =>
            {
                option.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                option.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                option.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                option.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login/";
                    options.AccessDeniedPath = "/Account/AccessDenied/";
                    options.LogoutPath = "/Account/Logout/";
                });

            //Added for session state
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".AdventureWorks.Session";
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
            });
            // The TempData provider cookie is not essential. Make it essential
            // so TempData is functional when tracking is disabled.
            services.Configure<CookieTempDataProviderOptions>(options =>
            {
                options.Cookie.IsEssential = true;
            });

            services.AddMemoryCache();
            services.AddMvc(option => option.EnableEndpointRouting = false).AddSessionStateTempDataProvider();
            services.AddControllersWithViews().AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);
            #region PostgreSqlConnection
            var postgreSqlConnectionString = Configuration.GetConnectionString("PostgreSqlConnectionString");
            services.AddEntityFrameworkNpgsql().AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(postgreSqlConnectionString, o => o.UseNetTopologySuite())
            );
            #endregion

            #region MongoDBConnection
            MongoDBSettings.AttachmentsCollectionName = Configuration["MongoDBSettings:AttachmentsCollectionName"];
            MongoDBSettings.ConnectionString = Configuration["MongoDBSettings:ConnectionString"];
            MongoDBSettings.DatabaseName = Configuration["MongoDBSettings:DatabaseName"];
            #endregion

            services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.SignIn.RequireConfirmedEmail = false;
            }).AddRoles<ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            services.AddControllersWithViews().AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);
            services.AddRazorPages().AddRazorRuntimeCompilation();

            services.AddAutoMapper(typeof(Startup));

            FCMSetting.ServerKey = Configuration["FCMSettings:ServerKey"];
            FCMSetting.SenderId = Configuration["FCMSettings:SenderId"];
            FCMSetting.FcmUrl = Configuration["FCMSettings:FcmUrl"];
            FirebaseSettings.App = Configuration["FCMSettings:Secret"];

            #region RegisterServices
            services.AddTransient<IStoresService, StoresService>();
            services.AddTransient<IUserService, UserService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddTransient(typeof(ISMSSenderService), typeof(SMSSenderRepository));
            services.AddTransient(typeof(INotificationsService), typeof(NotificationRepository));

            services.AddSingleton<StorageService>();
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();


            app.UseRouting();
            app.UseSession();
            app.UseCookiePolicy();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}

