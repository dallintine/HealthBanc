using Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence
{
    public class Seed
    {
        public static async Task SeedData(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if(userManager.FindByEmailAsync("hassan.hassan@sterling.ng") == null)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    UniqueUsername = "hassannh",
                    UserName = "hassan.hassan@sterling.ng",
                    Email = "hassan.hassan@sterling.ng",
                    FirstName = "Hassan",
                    LastName = "Hassan",
                    EmailConfirmed = true
                };
                BackendAdminUser adminUser = new BackendAdminUser()
                {
                    Email = "hassan.hassan@sterling.ng",
                    FirstName = "Hassan",
                    LastName = "Hassan",
                    ClassOrRoleId = 6
                };

                var result = userManager.CreateAsync(user).Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Super-Administrator").Wait();
                    context.BackendAdminUsers.Add(adminUser);
                    await context.SaveChangesAsync();
                }
            }
            if (userManager.FindByEmailAsync("Esther.nwowo@sterling.ng") == null)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    UniqueUsername = "nwowore",
                    UserName = "Esther.nwowo@sterling.ng",
                    Email = "Esther.nwowo@sterling.ng",
                    FirstName = "Esther",
                    LastName = "Nwowo",
                    EmailConfirmed = true
                };
                BackendAdminUser adminUser = new BackendAdminUser()
                {
                    Email = "Esther.nwowo@sterling.ng",
                    FirstName = "Esther",
                    LastName = "Nwowo",
                    ClassOrRoleId = 6
                };

                var result = userManager.CreateAsync(user).Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Super-Administrator").Wait();
                    context.BackendAdminUsers.Add(adminUser);
                    await context.SaveChangesAsync();
                }
            }
            if (userManager.FindByEmailAsync("constance.okosodo@sterling.ng") == null)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    UniqueUsername = "Okosodoec",
                    UserName = "constance.okosodo@sterling.ng",
                    Email = "constance.okosodo@sterling.ng",
                    FirstName = "Constance",
                    LastName = "Okosodo",
                    EmailConfirmed = true
                };
                BackendAdminUser adminUser = new BackendAdminUser()
                {
                    Email = "constance.okosodo@sterling.ng",
                    FirstName = "Constance",
                    LastName = "Okosodo",
                    ClassOrRoleId = 6
                };

                var result = userManager.CreateAsync(user).Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Super-Administrator").Wait();
                    context.BackendAdminUsers.Add(adminUser);
                    await context.SaveChangesAsync();
                }
            }

            await Task.CompletedTask;
        }
    }
}