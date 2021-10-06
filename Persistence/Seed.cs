using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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


            //foreach (var item in roles)
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
            //    var roleExist = await context.ClassOrRoles.Where(x => x.Name == item.Name).FirstOrDefaultAsync();
            //    if (roleExist == null)
            //    {
            //        await context.ClassOrRoles.AddAsync(item);
            //    }
            //}

            //var classRolesCount = await context.ClassOrRoles.ToListAsync();

            //if (classRolesCount.Count > 9)
            //{
            //    var oustedRoles = await context.ClassOrRoles.Where(x => x.Id > 9).ToListAsync();
            //    context.ClassOrRoles.RemoveRange(oustedRoles);
            //}

            //var appServices = new List<Service>()
            //{
            //    new Service() { Name="HealthMall"},
            //    new Service() {Name="HealthInsured"}
            //};

            //foreach (var item in appServices)
            //{
            //    var serviceExist = await context.Services.Where(x => x.Name == item.Name).FirstOrDefaultAsync();
            //    if (serviceExist == null)
            //    {
            //        await context.Services.AddAsync(item);
            //    }
            //}

            //var serviceCount = await context.Services.ToListAsync();

            //if (serviceCount.Count > 2)
            //{
            //    var oustedService = await context.Services.Where(x => x.Id > 2).ToListAsync();
            //    context.Services.RemoveRange(oustedService);
            //}
            //await context.SaveChangesAsync();

            var session = await context.UserSessions.ToListAsync();
            context.RemoveRange(session);
            await context.SaveChangesAsync();


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
                    var admin = await context.BackendAdminUsers.FirstOrDefaultAsync(x => x.Email == adminUser.Email);
                    if (admin is null)
                    {
                        context.BackendAdminUsers.Add(adminUser);
                    }
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

                    var adminUser =await context.BackendAdminUsers.FirstOrDefaultAsync(x => x.Email == "hassan.hassan@sterling.ng");
                    if(adminUser is null)
                    {
                        BackendAdminUser adminUser2 = new BackendAdminUser()
                        {
                            Email = "hassan.hassan@sterling.ng",
                            FirstName = "Hassan",
                            LastName = "Hassan",
                            ClassOrRoleId = 6
                        };
                        context.BackendAdminUsers.Add(adminUser2);
                    }
                    else
                    {
                        adminUser.ClassOrRoleId = 6;
                        context.BackendAdminUsers.Update(adminUser);
                    }
                    
                    await context.SaveChangesAsync();
                }
            }

            //var financeData = new List<HealthFinance>()
            //{
            //    new HealthFinance() {Name ="Hassan Hassan", Email="hassan.olatade.hh@gmail.com", BusinessName="Hassan Pharm", BusinessType="Finance",BusinessAddress="Lagos",Phonenumber="07034770338",
            //    Amount=decimal.Parse("200034"),Comment="Hassan",DateSubmitted=DateTime.Now},
            //    new HealthFinance() {Name ="Femi Alayaki", Email="FemiAlayaki@gmail.com", BusinessName="Femi Pharm", BusinessType="Finance",BusinessAddress="Ibadan",Phonenumber="07034776738",
            //    Amount=decimal.Parse("20000000"),Comment="Femi",DateSubmitted=DateTime.Now.AddDays(3)},
            //    new HealthFinance() {Name ="Bassey Effiong", Email="bassey@gmail.com", BusinessName="Bassey Microfinance bank", BusinessType="Banking",BusinessAddress="Calabar",Phonenumber="07045776738",
            //    Amount=decimal.Parse("300200000"),Comment="Bassey",DateSubmitted=DateTime.Now.AddDays(10)},
            //     new HealthFinance() {Name ="Constance Enon", Email="Enon@gmail.com", BusinessName="Constance Cosmetics", BusinessType="Body care",BusinessAddress="Lagos",Phonenumber="07045376738",
            //    Amount=decimal.Parse("400000"),Comment="Constance",DateSubmitted=DateTime.Now.AddDays(5)}
            //};
            //foreach (var item in financeData)
            //{
            //    var dataExist = await context.HealthFinances.Where(x => x.Email == item.Email).FirstOrDefaultAsync();
            //    if (dataExist is null)
            //    {
            //        await context.HealthFinances.AddAsync(item);
            //    }
            //}
            //await context.SaveChangesAsync();
            await Task.CompletedTask;            
        }
    }
}