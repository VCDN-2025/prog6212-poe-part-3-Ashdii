using CMCSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace CMCSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Controllers with views
            builder.Services.AddControllersWithViews();

            // Session setup - FIXED
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = "CMCSystem.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Use None for development
            });

            var app = builder.Build();

            // Pipeline - FIXED ORDER
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // CORRECT ORDER
            app.UseSession();         // Session must come before Authentication/Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}