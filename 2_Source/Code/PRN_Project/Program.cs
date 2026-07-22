using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký AppDbContext với chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Cấu hình dịch vụ Email
builder.Services.AddScoped<PRN_Project.Services.IEmailService, PRN_Project.Services.SmtpEmailService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(context);

    // Tự động khôi phục lại Email nội bộ
    var adminUser = context.Users.FirstOrDefault(u => u.UserCode == "ADMIN001");
    if (adminUser != null && adminUser.Email != "admin@cems.com")
    {
        adminUser.Email = "admin@cems.com";
    }

    // Cập nhật cho Technician
    var techUser = context.Users.FirstOrDefault(u => u.UserCode == "TECH001");
    if (techUser != null && techUser.Email != "tech@cems.com")
    {
        techUser.Email = "tech@cems.com";
    }

    // Cập nhật cho Lecturer
    var lecUser = context.Users.FirstOrDefault(u => u.UserCode == "LEC001");
    if (lecUser != null && lecUser.Email != "lecturer@cems.com")
    {
        lecUser.Email = "lecturer@cems.com";
    }

    context.SaveChanges();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
