using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace productApi.SeedUser
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // .env dosyasından oku
            string adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            string adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            string customerEmail = Environment.GetEnvironmentVariable("CUSTOMER_EMAIL");
            string customerPassword = Environment.GetEnvironmentVariable("CUSTOMER_PASSWORD");

            string[] roles = { "Admin", "Customer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Admin oluştur
            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var newAdmin = new IdentityUser
                    {
                        UserName = "admin",
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(newAdmin, adminPassword);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            // Customer oluştur
            if (!string.IsNullOrEmpty(customerEmail) && !string.IsNullOrEmpty(customerPassword))
            {
                var customerUser = await userManager.FindByEmailAsync(customerEmail);
                if (customerUser == null)
                {
                    var newCustomer = new IdentityUser
                    {
                        UserName = "customer",
                        Email = customerEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(newCustomer, customerPassword);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(newCustomer, "Customer");
                }
            }
        }
    }
}