using Microsoft.EntityFrameworkCore;
using RaceDay.Data; // Adjust this namespace if your AppDbContext is in another folder/namespace

namespace RaceDay
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add DbContext with SQL Server Connection String
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 2. Add Session Services (Required for Requirement 2 Session Management)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // 3. Add Controllers and OpenAPI / Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // 4. Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // 5. Enable Session Middleware (MUST be placed before UseAuthorization)
            app.UseSession();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}