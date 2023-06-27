using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.Data;

namespace Persistence.Seed
{
    public class Seed
    {
        public static async Task SeedData(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager
            ,IConfiguration configuration)
        {
            var email = configuration["DefaultAdmin:Email"];
            var user = await userManager.FindByEmailAsync($"{email}.admin");
            if (user is null)
            {               
                user = new ApplicationUser()
                {
                    UserName = email,
                    Email = $"{email}.admin",
                    FirstName = configuration["DefaultAdmin:FirstName"],
                    LastName = configuration["DefaultAdmin:LastName"],
                    EmailConfirmed = true
                };

                var result = userManager.CreateAsync(user).Result;

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user,Roles.Admin.ToString());
                    await context.SaveChangesAsync();
                }
            }
            
            var roles = Enum.GetValues(typeof(Roles))
                            .Cast<Roles>()
                            .ToList();

            foreach (var item in roles)
            {
                var roleExist = await roleManager.RoleExistsAsync(item.ToString());
                if (!roleExist)
                {
                    var role = new ApplicationRole
                    {
                        Name = item.ToString(),
                        NormalizedName = item.ToString().ToUpper()
                    };
                    await roleManager.CreateAsync(role);
                }
            }

            var service = await context.Services.SingleOrDefaultAsync(x => x.Name == "Gym Services");
            if (service == null)
            {
                service = new Service
                {
                    Name = "Gym Services",
                    Tag = "Get a one-time discount on your Gym Registration Fee, Pay ₦9,681.25 instead of ₦19,950",
                    ImageUrl = "https://pharmhallstracct.blob.core.windows.net/revampimages/7f1e7013-99b6-4937-9342-550decfb9aae_dumbell.png",
                    ServiceDetailURL = "https://pharmhallstracct.blob.core.windows.net/revampimages/f5ba27e0-9642-4cc1-bb97-4524ec5476e7_gymproductdetail.png",
                    BackgroundColor = "#FFF7ED",
                    ActiveColor = "#ecb13b"
                };
                context.Services.Add(service);
                await context.SaveChangesAsync();

                var vendor = await context.Vendors.SingleOrDefaultAsync(x => x.Name == "i-fitness");
                if (vendor is null)
                {
                    vendor = new Vendor
                    {
                        Name = "i-fitness",
                        SettlementAccount = "0076525143",
                        ServiceId = service.Id
                    };
                    context.Vendors.Add(vendor);
                    await context.SaveChangesAsync();

                    var plans = new List<Plan>
                {
                    new Plan
                    {
                        Tag = "Monthly Plan",
                        Price = 17394.83M,
                        MarkUpRate = 0.05,
                        VendorId = vendor.Id,
                        Name = "Monthly Fitness Package",
                        ImageURL ="https://pharmhallstracct.blob.core.windows.net/revampimages/a30e23f4-1cda-4d10-9c7f-689533182cd0_monthlygym.png",
                        PlanDescriptions = new List<PlanDescription>
                        {
                            new PlanDescription
                            {
                                Name = "All Day Access"
                            },
                             new PlanDescription
                            {
                                Name = "40+ Free Group Classes"
                            },
                              new PlanDescription
                            {
                                Name = "NO Guest Passes"
                            },
                               new PlanDescription
                            {
                                Name = "NO Freeze Subscription Request."
                            }
                        },
                        ServiceId= service.Id

                    },
                    new Plan
                    {
                        Tag = "Quarterly Plan",
                        Price = 43251.60M,
                        MarkUpRate = 0.05,
                        VendorId = vendor.Id,
                        Name = "Quarterly Fitness Package",
                          ImageURL ="https://pharmhallstracct.blob.core.windows.net/revampimages/c351e468-eda3-461b-87b7-47fb43483d16_quarterlygym.png",
                         PlanDescriptions = new List<PlanDescription>
                        {
                            new PlanDescription
                            {
                                Name = "All Day Access"
                            },
                             new PlanDescription
                            {
                                Name = "40+ Free Group Classes"
                            },
                              new PlanDescription
                            {
                                Name = "One Guest Pass per Month"
                            },
                               new PlanDescription
                            {
                                Name = "10 Days Per Annum Freeze Subscription Request."
                            }
                        },
                        ServiceId= service.Id
                    },
                    new Plan
                    {
                        Tag = "Annually Plan",
                        Price = 134865.15M,
                        MarkUpRate = 0.05,
                        VendorId = vendor.Id,
                        Name = "Annual Fitness Package",
                          ImageURL ="https://pharmhallstracct.blob.core.windows.net/revampimages/42f53767-5478-48a7-9d40-cddea933f0a0_yearlygym.png",
                        PlanDescriptions = new List<PlanDescription>
                        {
                            new PlanDescription
                            {
                                Name = "All Day Access"
                            },
                             new PlanDescription
                            {
                                Name = "40+ Free Group Classes"
                            },
                              new PlanDescription
                            {
                                Name = "Two Guest Pass per Month"
                            },
                               new PlanDescription
                            {
                                Name = "20 Days Per Annum Freeze Subscription Request."
                            }
                        },
                        ServiceId= service.Id
                    },
                    new Plan
                    {
                        Name = "Registration Fee",
                        Price = 8342.25M,
                        MarkUpRate = 0.05,
                        VendorId = vendor.Id,
                        ServiceId= service.Id,
                        OptionalFee = true,
                          PlanDescriptions = new List<PlanDescription>
                        {
                            new PlanDescription
                            {
                                Name = "I am new to this Gym - Add Registration Fee  8,342.25 is one of fee"
                            }
                        },
                    }
                };
                    context.Plans.AddRange(plans);
                    await context.SaveChangesAsync();
                }
            }

            var secondService = await context.Services.SingleOrDefaultAsync(x => x.Name == "Healthy Meal Service");
            if (secondService == null)
            {
                secondService = new Service
                {
                    Name = "Healthy Meal Service",
                    Tag = "Customers of Healthbanc will receive discounts on individual purchases of healthy food as well as monthly meal plan packages.",
                    ImageUrl = "https://pharmhallstracct.blob.core.windows.net/revampimages/a7365e84-d66e-4497-8f22-830d4bebc6ed_bibimap.png",
                    ServiceDetailURL = "https://pharmhallstracct.blob.core.windows.net/revampimages/e2ac630e-8ec2-43da-93dc-6de30263be8d_mealproductdetail.png",
                    BackgroundColor = "#EDFFFC",
                    ActiveColor = "#08321A"
                };
                context.Services.Add(secondService);
                await context.SaveChangesAsync();

                var secondVendor = await context.Vendors.SingleOrDefaultAsync(x => x.Name == "So fresh");
                if (secondVendor is null)
                {
                    secondVendor = new Vendor
                    {
                        Name = "So fresh",
                        SettlementAccount = "0076525143",
                        ServiceId = secondService.Id
                    };
                    context.Vendors.Add(secondVendor);
                    await context.SaveChangesAsync();

                    var plans2 = new List<Plan>
                {
                    new Plan
                    {
                        Name = "Fresh Start",
                        Price = 45465.00M,
                        MarkUpRate = 0.05,
                        ImageURL ="https://pharmhallstracct.blob.core.windows.net/revampimages/12901956-9153-4c24-86fc-02aa2effac1e_freshstart.png",
                        VendorId = secondVendor.Id,
                        ExternalLinkName = "View Meal Plan",
                        ExternalLinkURL = "https://drive.google.com/file/d/1FGvmw3xVTnS25VGMf3wS7C54flQM2UaZ/view",
                        ServiceId= secondService.Id,
                         PlanDescriptions = new List<PlanDescription>
                        {
                            new PlanDescription
                            {
                                Name = "Amount includes delivery fee for 20 days; Please, click on link to see meal plan"
                            }
                        },
                    },
                    new Plan
                    {
                        Name = "Yummy Feast",
                        Price = 91654.50M,
                        MarkUpRate = 0.05,
                        ImageURL ="https://pharmhallstracct.blob.core.windows.net/revampimages/c54a0dbb-1981-4c04-a2fb-f6da0da8c620_yummyfeast.png",
                        VendorId = secondVendor.Id,
                        ExternalLinkName = "View Meal Plan",
                        ExternalLinkURL = "https://drive.google.com/file/d/1GEF54enGXotwfFGC7ovZ-Z92p25M7JPq/view",
                        ServiceId= secondService.Id, 
                        PlanDescriptions = new List<PlanDescription>
                        {
                            new PlanDescription
                            {
                                Name = "Amount includes delivery fee for 20 days; Please, click on link to see meal plan"
                            }
                        },
                    },
                    new Plan
                    {
                        Name = "Exotic Bliss",
                        Price = 102648.00M,
                        MarkUpRate = 0.05,
                        ImageURL ="https://pharmhallstracct.blob.core.windows.net/revampimages/0708e25b-5033-477d-984e-800e4a24bbab_exoticbliss.png",
                        VendorId = secondVendor.Id,
                        ExternalLinkName = "View Meal Plan",
                        ExternalLinkURL = "https://drive.google.com/file/d/1ywGmFZiGDBbM4iOiSjLaahBft5NKmoOo/view",
                        ServiceId= secondService.Id,
                        PlanDescriptions = new List<PlanDescription>
                        {
                            new PlanDescription
                            {
                                Name = "Amount includes delivery fee for 20 days; Please, click on link to see meal plan"
                            }
                        },
                    },
                    new Plan
                    {
                        Name = "Average Meal",
                        Price = 5250.00M,
                        MarkUpRate = 0.05,
                        VendorId = secondVendor.Id,
                        ExternalLinkName = "View Meal Plan",
                        ServiceId= secondService.Id
                    }
                };
                    context.Plans.AddRange(plans2);
                    await context.SaveChangesAsync();
                }
            }

            var thirdService = await context.Services.SingleOrDefaultAsync(x => x.Name == "Diagnostics Service");
            if (thirdService == null)
            {
                thirdService = new Service
                {
                    Name = "Diagnostics Service",
                    Tag = "Enjoy a discount on all services, starting with a full body medical checkup and sexually transmitted disease test.",
                    ImageUrl = "https://pharmhallstracct.blob.core.windows.net/revampimages/7fe26149-b122-4ebf-8f15-e90cc31e16e0_diagnostic.png",
                    BackgroundColor = "#FFEDED",
                    ActiveColor = "#D71E1B"
                };
                context.Services.Add(thirdService);
                await context.SaveChangesAsync();

                var thirdVendor = await context.Vendors.SingleOrDefaultAsync(x => x.Name == "Healthtracka");
                if (thirdVendor is null)
                {
                    thirdVendor = new Vendor
                    {
                        Name = "Healthtracka",
                        SettlementAccount = "0076525143",
                        ServiceId = thirdService.Id
                    };
                    context.Vendors.Add(thirdVendor);
                    await context.SaveChangesAsync();

                    var plans3 = new List<Plan>
                    {
                        new Plan
                        {
                            Name = "Bronze Package",
                            Price = 21000.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id,
                            ServiceId=thirdService.Id,
                            PlanDescriptions = new List<PlanDescription>
                            {
                                new PlanDescription
                                {
                                    Name = "Fasting Blood Sugar, Total Cholesterol, Full Blood Count,Urinalysis, Liver Function Test, Kidney function Test and Logistics fee"
                                }
                            }
                        },
                        new Plan
                        {
                            Name = "Silver Package",
                            Price = 49350.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id,
                            ServiceId=thirdService.Id,
                            PlanDescriptions = new List<PlanDescription>
                            {
                                new PlanDescription
                                {
                                    Name = "All Bronze and Silver Test included +HbA1C, Inorganic Phosphate, Calcium, C-reactive Protein, Hepatitis B Surface Antigen Rapid, HIV I & II Rapid, Hepatitis C virus AP rapid, and Stool Occult Blood"
                                }
                            }

                        },
                        new Plan
                        {
                            Name = "Gold Package",
                            Price = 96600.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id,
                            ServiceId=thirdService.Id,
                            PlanDescriptions = new List<PlanDescription>
                            {
                                new PlanDescription
                                {
                                    Name = "  All Bronze and Silver Test included +HbA1C, Inorganic Phosphate, Calcium, C-reactive Protein, Hepatitis B Surface Antigen Rapid, HIV I & II Rapid, Hepatitis C virus AP rapid, and Stool Occult Blood"
                                }
                            }                           
                        },
                        new Plan
                        {
                            Name = "STD Lemon",
                            Price = 21000.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id,
                            ServiceId=thirdService.Id,
                             PlanDescriptions = new List<PlanDescription>
                            {
                                new PlanDescription
                                {
                                    Name = " Hepatitis B Surface Antigen Rapid, HIV I & II Rapid, Syphilis Screening, Urinalysis, Neisseria Gonorrhoea Rapid and logistics Fee"
                                }
                            }                           
                        },
                        new Plan
                        {
                            Name = "STD Lemononade",
                            Price = 39900.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id,
                            ServiceId=thirdService.Id,
                             PlanDescriptions = new List<PlanDescription>
                            {
                                new PlanDescription
                                {
                                    Name = " All lemon test included + Hepatitis C Virus Rapid, Urine Microscopy, culture & sensitivity and Chlamydia Trachoma's Rapid"
                                }
                            }
                        },
                        new Plan
                        {
                            Name = "STD Lemmonade Plus",
                            Price = 77700.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id,
                            ServiceId=thirdService.Id,
                            PlanDescriptions = new List<PlanDescription>
                            {
                                new PlanDescription
                                {
                                    Name = "All lemon and Lemonade test included + Chlamydia IgM AB and Herpes Simplex I and II"
                                }
                            }
                        }
                    };
                    context.Plans.AddRange(plans3);
                    await context.SaveChangesAsync();
                }
            }          

        }
    }
}










