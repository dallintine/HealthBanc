using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
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

            //var session = await context.UserSessions.ToListAsync();
            //context.RemoveRange(session);
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
            await Task.CompletedTask;

            // Individual Users
            var users = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test1@gmail.com",Email = "test1@gmail.com",FirstName = "test1",LastName = "test1",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test2@gmail.com",Email = "test2@gmail.com",FirstName = "test2",LastName = "test2",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test3@gmail.com",Email = "test3@gmail.com",FirstName = "test3",LastName = "test3",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test17@gmail.com",Email = "test17@gmail.com",FirstName = "test17",LastName = "test17",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test18@gmail.com",Email = "test18@gmail.com",FirstName = "test18",LastName = "test18",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test19@gmail.com",Email = "test19@gmail.com",FirstName = "test19",LastName = "test19",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test20@gmail.com",Email = "test20@gmail.com",FirstName = "test20",LastName = "test20",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test21@gmail.com",Email = "test21@gmail.com",FirstName = "test21",LastName = "test21",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test22@gmail.com",Email = "test22@gmail.com",FirstName = "test22",LastName = "test22",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},

            };

            foreach (var item in users)
            {
                var result2 = await userManager.CreateAsync(item, "Asdflkj2468.");
                if (result2.Succeeded)
                {
                    item.EmailConfirmed = true;
                    await userManager.UpdateAsync(item);
                    await userManager.AddToRoleAsync(item, "SuperAdmin");
                    await userManager.UpdateAsync(item);
                }

                var insuranceProfile = new InsuranceUserProfile()
                {
                    UserId = item.Id,
                    Surname = item.LastName,
                    Othernames = item.LastName,
                    DateOfBirth = DateTime.Now,
                    MaritalStatus = "Single",
                    Gender = "Male",
                    PhoneNumber = item.PhoneNumber,
                    Email = item.Email,
                    PlanCode = "1",
                    Premium = Decimal.Parse("1000"),
                    SubscriptionStatus = true,
                    ActiveStatus = true,
                    InsuranceService = "Hygeia",
                    ContactAddress = "lagos",
                    CareProviderName = "test test",
                    StateOfResidence = "lagos",
                    TownOfResidence = "yaba",
                    StartActiveStatusDate = new DateTime(2021, 9, 25),
                    EndActiveStatusDate = new DateTime(2021, 9, 25).AddDays(28),
                    TransId = "0989768965",
                    PendingJobId = "23",
                    PendingEmailJobId = "25"
                };

                context.InsuranceUserProfiles.Add(insuranceProfile);
                await context.SaveChangesAsync();

                var card = new DebitCard();
                if (item.Email == "test1@gmail.com")
                {
                    card = new DebitCard(item.Id, insuranceProfile.Id, null, null, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_bpyozf6515");
                }
                else if (item.Email == "test2@gmail.com")
                {
                    card = new DebitCard(item.Id, insuranceProfile.Id, null, null, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_ysa2h6rlwe");
                }
                else
                {
                    card = new DebitCard(item.Id, insuranceProfile.Id, null, null, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_yxk6i3q6r2");
                }
                context.Cards.Add(card);
                await context.SaveChangesAsync();

                var completionProfile = new InsuranceCompletionProfile(item.Id, true, true, insuranceProfile.InsuranceService);
                context.InsuranceCompletionProfiles.Add(completionProfile);
                await context.SaveChangesAsync();
            }

            var users2 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test4@gmail.com",Email = "test4@gmail.com",FirstName = "test4",LastName = "test4",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test5@gmail.com",Email = "test5@gmail.com",FirstName = "test5",LastName = "test5",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
            };

            foreach (var item in users2)
            {
                var result2 = await userManager.CreateAsync(item, "Asdflkj2468.");
                if (result2.Succeeded)
                {
                    item.EmailConfirmed = true;
                    await userManager.UpdateAsync(item);
                    await userManager.AddToRoleAsync(item, "SuperAdmin");
                    await userManager.UpdateAsync(item);
                }

                var insuranceProfile = new InsuranceUserProfile()
                {
                    UserId = item.Id,
                    Surname = item.LastName,
                    Othernames = item.LastName,
                    DateOfBirth = DateTime.Now,
                    MaritalStatus = "Single",
                    Gender = "Male",
                    PhoneNumber = item.PhoneNumber,
                    Email = item.Email,
                    PlanCode = "1",
                    Premium = Decimal.Parse("1000"),
                    SubscriptionStatus = true,
                    ActiveStatus = true,
                    InsuranceService = "Hygeia",
                    ContactAddress = "lagos",
                    CareProviderName = "test test",
                    StateOfResidence = "lagos",
                    TownOfResidence = "yaba",
                    StartActiveStatusDate = new DateTime(2021, 11, 5),
                    EndActiveStatusDate = new DateTime(2021, 11, 5).AddDays(28),
                    TransId = "0989768965",
                    PendingJobId = "23",
                    PendingEmailJobId = "25"
                };

                context.InsuranceUserProfiles.Add(insuranceProfile);
                await context.SaveChangesAsync();

                var card = new DebitCard();
                if (item.Email == "test4@gmail.com")
                {
                    card = new DebitCard(item.Id, insuranceProfile.Id, null, null, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_vcljv8t0gx");
                }
                else if (item.Email == "test5@gmail.com")
                {
                    card = new DebitCard(item.Id, insuranceProfile.Id, null, null, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_oqk3eogdvl");
                }
                context.Cards.Add(card);
                await context.SaveChangesAsync();

                var completionProfile = new InsuranceCompletionProfile(item.Id, true, true, insuranceProfile.InsuranceService);
                context.InsuranceCompletionProfiles.Add(completionProfile);
                await context.SaveChangesAsync();
            }

            var users3 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test6@gmail.com",Email = "test6@gmail.com",FirstName = "test6",LastName = "test6",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},

            };

            foreach (var item in users3)
            {
                var result2 = await userManager.CreateAsync(item, "Asdflkj2468.");
                if (result2.Succeeded)
                {
                    item.EmailConfirmed = true;
                    await userManager.UpdateAsync(item);
                    await userManager.AddToRoleAsync(item, "SuperAdmin");
                    await userManager.UpdateAsync(item);
                }

                var insuranceProfile = new InsuranceUserProfile()
                {
                    UserId = item.Id,
                    Surname = item.LastName,
                    Othernames = item.LastName,
                    DateOfBirth = DateTime.Now,
                    MaritalStatus = "Single",
                    Gender = "Male",
                    PhoneNumber = item.PhoneNumber,
                    Email = item.Email,
                    PlanCode = "1",
                    Premium = Decimal.Parse("1000"),
                    SubscriptionStatus = true,
                    ActiveStatus = true,
                    InsuranceService = "Hygeia",
                    ContactAddress = "lagos",
                    CareProviderName = "test test",
                    StateOfResidence = "lagos",
                    TownOfResidence = "yaba",
                    StartActiveStatusDate = new DateTime(2021, 12, 25),
                    EndActiveStatusDate = new DateTime(2021, 12, 25).AddDays(28),
                    TransId = "0989768965",
                    PendingJobId = "23",
                    PendingEmailJobId = "25"
                };

                context.InsuranceUserProfiles.Add(insuranceProfile);
                await context.SaveChangesAsync();

                var card = new DebitCard();
                if (item.Email == "test6@gmail.com")
                {
                    card = new DebitCard(item.Id, insuranceProfile.Id, null, null, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_r6t0u2e2v5");
                }

                context.Cards.Add(card);
                await context.SaveChangesAsync();

                var completionProfile = new InsuranceCompletionProfile(item.Id, true, true, insuranceProfile.InsuranceService);
                context.InsuranceCompletionProfiles.Add(completionProfile);
                await context.SaveChangesAsync();
            }

            // Family
            var users4 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test7@gmail.com",Email = "test7@gmail.com",FirstName = "test7",LastName = "test7",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
            };

            foreach (var item in users4)
            {
                var result2 = await userManager.CreateAsync(item, "Asdflkj2468.");
                if (result2.Succeeded)
                {
                    await userManager.UpdateAsync(item);
                    item.EmailConfirmed = true;
                    await userManager.AddToRoleAsync(item, "SuperAdmin");
                    await userManager.UpdateAsync(item);
                }

                var familyProfile = new FamilyProfile()
                {
                    UserId = item.Id,
                    PhoneNumber = item.PhoneNumber,
                    Email = item.Email,
                    FullName = item.LastName + item.FirstName,
                    InsuranceService = "Hygeia",
                    EmailConfirmed = true,
                    TokenizationCompleted = true,
                    ProfileCompleted = true,
                    DateCreated = DateTime.Now,
                    PendingJobId = "23",
                    PendingEmailJobId = "25"
                };
                context.FamilyProfiles.Add(familyProfile);
                await context.SaveChangesAsync();

                var card = new DebitCard();
                if (item.Email == "test7@gmail.com")
                {
                    card = new DebitCard(item.Id, null, null, familyProfile.Id, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_87q4acs3h8");
                }

                context.Cards.Add(card);
                await context.SaveChangesAsync();
            }

            // Add Family Member

            var users5 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test8@gmail.com",FirstName = "test8",LastName = "test8",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test9@gmail.com",FirstName = "test9",LastName = "test9",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test10@gmail.com",FirstName = "test9",LastName = "test9",DateOfRegistration = DateTime.Now},

            };

            foreach (var item in users5)
            {
                var family = context.FamilyProfiles.Where(x => x.Email == "test7@gmail.com").FirstOrDefault();

                if (item.UserName == "test8@gmail.com")
                {
                    var insuranceProfile = new InsuranceUserProfile()
                    {
                        FamilyProfileId = family.Id,
                        PhoneNumber = family.PhoneNumber,
                        FamilyEmail = family.Email,
                        Surname = item.LastName,
                        Othernames = item.LastName,
                        DateOfBirth = DateTime.Now,
                        MaritalStatus = "Single",
                        Gender = "Male",
                        Email = item.Email,
                        PlanCode = "1",
                        Premium = Decimal.Parse("1000"),
                        SubscriptionStatus = true,
                        ActiveStatus = true,
                        InsuranceService = InsuranceProvider.Axamansard.ToString(),
                        ContactAddress = "lagos",
                        CareProviderName = "test test",
                        StateOfResidence = "lagos",
                        TownOfResidence = "yaba",
                        StartActiveStatusDate = new DateTime(2021, 10, 25),
                        EndActiveStatusDate = new DateTime(2021, 10, 25).AddDays(28),
                        TransId = "0989768965",
                        PendingJobId = "23",
                        PendingEmailJobId = "25"
                    };
                    context.InsuranceUserProfiles.Add(insuranceProfile);
                    await context.SaveChangesAsync();
                }
                else if (item.UserName == "test10@gmail.com")
                {
                    var insuranceProfile = new InsuranceUserProfile()
                    {
                        FamilyProfileId = family.Id,
                        PhoneNumber = family.PhoneNumber,
                        FamilyEmail = family.Email,
                        Surname = item.LastName,
                        Othernames = item.LastName,
                        DateOfBirth = DateTime.Now,
                        MaritalStatus = "Single",
                        Gender = "Male",
                        Email = item.Email,
                        PlanCode = "1",
                        Premium = Decimal.Parse("1000"),
                        SubscriptionStatus = null,
                        ActiveStatus = null,
                        InsuranceService = InsuranceProvider.Axamansard.ToString(),
                        ContactAddress = "lagos",
                        CareProviderName = "test test",
                        StateOfResidence = "lagos",
                        TownOfResidence = "yaba"
                    };
                    context.InsuranceUserProfiles.Add(insuranceProfile);
                    await context.SaveChangesAsync();
                }
                else
                {
                    var insuranceProfile = new InsuranceUserProfile()
                    {
                        FamilyProfileId = family.Id,
                        PhoneNumber = family.PhoneNumber,
                        FamilyEmail = family.Email,
                        Surname = item.LastName,
                        Othernames = item.LastName,
                        DateOfBirth = DateTime.Now,
                        MaritalStatus = "Single",
                        Gender = "Male",
                        Email = item.Email,
                        PlanCode = "1",
                        Premium = Decimal.Parse("1000"),
                        SubscriptionStatus = true,
                        ActiveStatus = true,
                        InsuranceService = InsuranceProvider.Axamansard.ToString(),
                        ContactAddress = "lagos",
                        CareProviderName = "test test",
                        StateOfResidence = "lagos",
                        TownOfResidence = "yaba",
                        StartActiveStatusDate = new DateTime(2022, 01, 2),
                        EndActiveStatusDate = new DateTime(2022, 01, 2).AddDays(28),
                        TransId = "0989768965",
                        PendingJobId = "23",
                        PendingEmailJobId = "25"
                    };
                    context.InsuranceUserProfiles.Add(insuranceProfile);
                    await context.SaveChangesAsync();
                }

            }

            // Test Refereee

            var users6 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test11@gmail.com",Email = "test11@gmail.com",FirstName = "test11",LastName = "test11",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
            };

            foreach (var item in users6)
            {
                var profile = context.InsuranceUserProfiles.Where(x => x.Email == "test6@gmail.com").FirstOrDefault();

                var insuranceProfile = new InsuranceUserProfile()
                {
                    InsurancePayeeId = profile.Id,
                    Surname = item.LastName,
                    Othernames = item.LastName,
                    PhoneNumber = item.PhoneNumber,
                    DateOfBirth = DateTime.Now,
                    MaritalStatus = "Single",
                    Gender = "Male",
                    Email = item.Email,
                    PlanCode = "1",
                    Premium = Decimal.Parse("1000"),
                    SubscriptionStatus = true,
                    ActiveStatus = true,
                    InsuranceService = "Hygeia",
                    ContactAddress = "lagos",
                    CareProviderName = "test test",
                    StateOfResidence = "lagos",
                    TownOfResidence = "yaba",
                    StartActiveStatusDate = new DateTime(2021, 10, 25),
                    EndActiveStatusDate = new DateTime(2021, 10, 25).AddDays(28),
                    TransId = "0989768965",
                    PendingJobId = "23",
                    PendingEmailJobId = "25"
                };
                context.InsuranceUserProfiles.Add(insuranceProfile);
                await context.SaveChangesAsync();
            }

            // Coporate

            var users7 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test12@gmail.com",Email = "test12@gmail.com",FirstName = "test12",LastName = "test12",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
            };


            foreach (var item in users7)
            {
                var result2 = await userManager.CreateAsync(item, "Asdflkj2468.");
                if (result2.Succeeded)
                {
                    item.EmailConfirmed = true;
                    await userManager.UpdateAsync(item);
                    await userManager.AddToRoleAsync(item, "SuperAdmin");
                    await userManager.UpdateAsync(item);
                }

                var company = new CompanyProfile()
                {
                    UserId = item.Id,
                    PendingJobId = "12",
                    PendingEmailJobId = "14",
                    CompanyName = "Hassan Pharm",
                    CompanyEmail = item.Email,
                    PhoneNumber = item.PhoneNumber,
                    Industry = "Pharmaceutical",
                    CompanySize = "12",
                    EmailConfirmed = true,
                    TokenizationCompleted = true,
                    ProfileCompleted = true,
                    InsuranceService = "Hygeia",
                    NextPaymentDate = new DateTime(2021, 12, 7)
                };

                context.CompanyProfiles.Add(company);
                await context.SaveChangesAsync();

                var card = new DebitCard(item.Id, null, company.Id, null, 1, "6666", "Visa", Guid.NewGuid().ToString(), "AUTH_yulec75efr");
                context.Cards.Add(card);
                await context.SaveChangesAsync();
            }

            // Company beneficiaries Active

            var users8 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test13@gmail.com",Email = "test13@gmail.com",FirstName = "test13",LastName = "test13",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test14@gmail.com",Email = "test14@gmail.com",FirstName = "test14",LastName = "test14",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
            };

            var coporate = context.CompanyProfiles.Where(x => x.CompanyEmail == "test12@gmail.com").FirstOrDefault();

            foreach (var item in users8)
            {
                var result2 = await userManager.CreateAsync(item, "Asdflkj2468.");
                if (result2.Succeeded)
                {
                    item.EmailConfirmed = true;
                    await userManager.UpdateAsync(item);
                    await userManager.AddToRoleAsync(item, "SuperAdmin");
                    await userManager.UpdateAsync(item);
                }

                var insuranceProfile = new InsuranceUserProfile()
                {
                    UserId = item.Id,
                    CompanyProfileId = coporate.Id,
                    CompanyName = coporate.CompanyName,
                    CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Active.ToString(),
                    Surname = item.LastName,
                    Othernames = item.LastName,
                    DateOfBirth = DateTime.Now,
                    MaritalStatus = "Single",
                    Gender = "Male",
                    PhoneNumber = item.PhoneNumber,
                    Email = item.Email,
                    PlanCode = "1",
                    Premium = Decimal.Parse("1000"),
                    SubscriptionStatus = true,
                    ActiveStatus = true,
                    InsuranceService = "Hygeia",
                    ContactAddress = "lagos",
                    CareProviderName = "test test",
                    StateOfResidence = "lagos",
                    TownOfResidence = "yaba",
                    StartActiveStatusDate = new DateTime(2021, 9, 25),
                    EndActiveStatusDate = new DateTime(2021, 9, 25).AddDays(28),
                    TransId = "0989768965",
                    PendingJobId = "23",
                    PendingEmailJobId = "25"
                };

                context.InsuranceUserProfiles.Add(insuranceProfile);
                await context.SaveChangesAsync();
            }

            // Company beneficiaries Pending

            var users9 = new List<ApplicationUser>
            {
                new ApplicationUser{UserName = "test15@gmail.com",Email = "test15@gmail.com",FirstName = "test15",LastName = "test15",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
                new ApplicationUser{UserName = "test16@gmail.com",Email = "test16@gmail.com",FirstName = "test16",LastName = "test16",PhoneNumber = "07034889227",DateOfRegistration = DateTime.Now},
            };

            var coporate2 = context.CompanyProfiles.Where(x => x.CompanyEmail == "test12@gmail.com").FirstOrDefault();

            foreach (var item in users9)
            {
                var result2 = await userManager.CreateAsync(item, "Asdflkj2468.");
                if (result2.Succeeded)
                {
                    item.EmailConfirmed = true;
                    await userManager.UpdateAsync(item);
                    await userManager.AddToRoleAsync(item, "SuperAdmin");
                    await userManager.UpdateAsync(item);
                }

                var insuranceProfile = new InsuranceUserProfile()
                {
                    UserId = item.Id,
                    CompanyProfileId = coporate.Id,
                    CompanyName = coporate.CompanyName,
                    CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Pending.ToString(),
                    Surname = item.LastName,
                    Othernames = item.LastName,
                    DateOfBirth = DateTime.Now,
                    MaritalStatus = "Single",
                    Gender = "Male",
                    PhoneNumber = item.PhoneNumber,
                    Email = item.Email,
                    PlanCode = "1",
                    Premium = Decimal.Parse("1000"),
                    SubscriptionStatus = true,
                    ActiveStatus = true,
                    InsuranceService = "Hygeia",
                    ContactAddress = "lagos",
                    CareProviderName = "test test",
                    StateOfResidence = "lagos",
                    TownOfResidence = "yaba",
                    StartActiveStatusDate = new DateTime(2021, 9, 25),
                    EndActiveStatusDate = new DateTime(2021, 9, 25).AddDays(28),
                    TransId = "0989768965",
                    PendingJobId = "23",
                    PendingEmailJobId = "25"
                };

                context.InsuranceUserProfiles.Add(insuranceProfile);
                await context.SaveChangesAsync();
            }

        }
    }
}