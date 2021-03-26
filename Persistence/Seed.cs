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
        public static async Task SeedData(ApplicationDbContext context, UserManager<ApplicationUser> userManager,RoleManager<AppRole> roleManager)
        {
            //var roles = new List<AppRole>() {
            //     new AppRole() { Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
            //     new AppRole() { Name = "Test2", NormalizedName = "TEST2" },
            //     new AppRole() { Name = "Test3", NormalizedName = "TEST3" },
            //     new AppRole() { Name = "Test4", NormalizedName = "TEST4" },
            //     new AppRole() { Name = "Test5", NormalizedName = "TEST5" },
            //     new AppRole() { Name = "Super-Administrator", NormalizedName = "SUPER-ADMINISTRATOR" },
            //     new AppRole() { Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
            //     new AppRole() { Name = "Technical-Support", NormalizedName = "TECHNICAL-SUPPORT" },
            //     new AppRole() { Name = "Analyst", NormalizedName = "ANALYST" }
            //};

            //foreach(var item in roles)
            //{
            //    var roleExist = await roleManager.RoleExistsAsync(item.Name);
            //    if (!roleExist)
            //    {
            //        await roleManager.CreateAsync(item);
            //    }
            //}

            //var classRoles = new List<ClassOrRole>()
            //{
            //   new ClassOrRole() { Name = "SuperAdmin" },
            //   new ClassOrRole() { Name = "Test2"},
            //   new ClassOrRole() { Name = "Test3"},
            //   new ClassOrRole() { Name = "Test4"},
            //   new ClassOrRole() { Name = "Test5" },
            //   new ClassOrRole() { Name = "Super-Administrator"},
            //   new ClassOrRole() { Name = "Administrator"},
            //   new ClassOrRole() { Name = "Technical-Support"},
            //   new ClassOrRole() { Name = "Analyst"}
            //};

            //foreach (var item in classRoles)
            //{
            //    var roleExist = await context.ClassOrRoles.FindAsync(item.Id);
            //    if (roleExist == null)
            //    {
            //        await context.ClassOrRoles.AddAsync(item);
            //    }
            //}


            //var appServices = new List<Service>()
            //{
            //    new Service() { Name="HealthMall"},
            //    new Service() {Name="HealthInsured"}
            //};

            //foreach (var item in appServices)
            //{
            //    var serviceExist = await context.Services.FindAsync(item.Id);
            //    if (serviceExist == null)
            //    {
            //        await context.Services.AddAsync(item);
            //    }
            //}

            //await context.SaveChangesAsync();


            if (await userManager.FindByEmailAsync("hassan.hassan@sterling.ng.admin") == null)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    UniqueUsername = "hassannh",
                    UserName = "hassan.hassan@sterling.ng.admin",
                    Email = "hassan.hassan@sterling.ng.admin",
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
            else
            {
                var user = await userManager.FindByEmailAsync("hassan.hassan@sterling.ng.admin");
                var checkRole = await userManager.IsInRoleAsync(user, "Super-Administrator");
                if(checkRole == false)
                {
                    userManager.AddToRoleAsync(user, "Super-Administrator").Wait();

                    BackendAdminUser adminUser = new BackendAdminUser()
                    {
                        Email = "hassan.hassan@sterling.ng",
                        FirstName = "Hassan",
                        LastName = "Hassan",
                        ClassOrRoleId = 6
                    };
                    context.BackendAdminUsers.Add(adminUser);
                    await context.SaveChangesAsync();
                }
            }

            if (await userManager.FindByEmailAsync("Esther.nwowo@sterling.ng.admin") == null)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    UniqueUsername = "nwowore",
                    UserName = "Esther.nwowo@sterling.ng.admin",
                    Email = "Esther.nwowo@sterling.ng.admin",
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
            else
            {
                var user = await userManager.FindByEmailAsync("Esther.nwowo@sterling.ng.admin");
                var checkRole = await userManager.IsInRoleAsync(user, "Super-Administrator");
                if (checkRole == false)
                {
                    userManager.AddToRoleAsync(user, "Super-Administrator").Wait();

                    BackendAdminUser adminUser = new BackendAdminUser()
                    {
                        Email = "Esther.nwowo@sterling.ng",
                        FirstName = "Esther",
                        LastName = "Nwowo",
                        ClassOrRoleId = 6
                    };
                    context.BackendAdminUsers.Add(adminUser);
                    await context.SaveChangesAsync();
                }
            }

            if (await userManager.FindByEmailAsync("constance.okosodo@sterling.ng.admin") == null)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    UniqueUsername = "Okosodoec",
                    UserName = "constance.okosodo@sterling.ng.admin",
                    Email = "constance.okosodo@sterling.ng.admin",
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
            else
            {
                var user = await userManager.FindByEmailAsync("constance.okosodo@sterling.ng.admin");
                var checkRole = await userManager.IsInRoleAsync(user, "Super-Administrator");
                if (checkRole == false)
                {
                    userManager.AddToRoleAsync(user, "Super-Administrator").Wait();

                    BackendAdminUser adminUser = new BackendAdminUser()
                    {
                        Email = "constance.okosodo@sterling.ng",
                        FirstName = "Constance",
                        LastName = "Okosodo",
                        ClassOrRoleId = 6
                    };
                    context.BackendAdminUsers.Add(adminUser);
                    await context.SaveChangesAsync();
                }
            }

            await Task.CompletedTask;
        }

        
    }
}