using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;

namespace Persistence.Seed
{
    public class Seed
    {
        public static async Task SeedData(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
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
                    ImageUrl = ""
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
                        }

                    },
                    new Plan
                    {
                        Tag = "Quarterly Plan",
                        Price = 43251.60M,
                        MarkUpRate = 0.05,
                        VendorId = vendor.Id,
                        Name = "Quarterly Fitness Package",
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
                        }
                    },
                    new Plan
                    {
                        Tag = "Annually Plan",
                        Price = 134865.15M,
                        MarkUpRate = 0.05,
                        VendorId = vendor.Id,
                        Name = "Annual Fitness Package",
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
                        }
                    },
                    new Plan
                    {
                        Name = "Registration Fee",
                        Price = 8342.25M,
                        MarkUpRate = 0.05,
                        VendorId = vendor.Id
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
                    ImageUrl = ""
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
                        VendorId = secondVendor.Id
                    },
                    new Plan
                    {
                        Name = "Yummy Feast",
                        Price = 91654.50M,
                        MarkUpRate = 0.05,
                        VendorId = secondVendor.Id
                    },
                    new Plan
                    {
                        Name = "Exotic Bliss",
                        Price = 102648.00M,
                        MarkUpRate = 0.05,
                        VendorId = secondVendor.Id
                    },
                    new Plan
                    {
                        Name = "Average Meal",
                        Price = 5250.00M,
                        MarkUpRate = 0.05,
                        VendorId = secondVendor.Id
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
                    ImageUrl = ""
                };
                context.Services.Add(thirdService);
                await context.SaveChangesAsync();

                var thirdVendor = await context.Vendors.SingleOrDefaultAsync(x => x.Name == "Healthtracka");
                if (thirdVendor is null)
                {
                    thirdVendor = new Vendor
                    {
                        Name = "Healthtracka",
                        SettlementAccount = "0076525143"
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
                            VendorId = thirdVendor.Id
                        },
                        new Plan
                        {
                            Name = "Silver Package",
                            Price = 49350.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id
                        },
                        new Plan
                        {
                            Name = "Gold Package",
                            Price = 96600.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id
                        },
                        new Plan
                        {
                            Name = "STD Lemon",
                            Price = 21000.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id
                        },
                        new Plan
                        {
                            Name = "STD Lemononade",
                            Price = 39900.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id
                        },
                        new Plan
                        {
                            Name = "STD Lemmonade Plus",
                            Price = 77700.00M,
                            MarkUpRate = 0.05,
                            VendorId = thirdVendor.Id
                        }
                    };
                    context.Plans.AddRange(plans3);
                    await context.SaveChangesAsync();
                }
            }          

        }
    }
}










