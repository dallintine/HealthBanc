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

            var product = await context.Products.SingleOrDefaultAsync(x => x.Name == "i-fitness");
            if(product is null)
            {
                product = new Product
                {
                    Name = "i-fitness",
                    SettlementAccount = "0076525143"
                };
                context.Products.Add(product);
                await context.SaveChangesAsync();

                var plans = new List<Plan>
                {
                    new Plan
                    {
                        Name = "Monthly Plan",
                        Price = 17394.83M,
                        MarkUpRate = 0.05,
                        ProductId = product.Id
                    },
                    new Plan
                    {
                        Name = "Quarterlu Plan",
                        Price = 43251.60M,
                        MarkUpRate = 0.05,
                        ProductId = product.Id
                    },
                    new Plan
                    {
                        Name = "Annually Plan",
                        Price = 134865.15M,
                        MarkUpRate = 0.05,
                        ProductId = product.Id
                    },
                    new Plan
                    {
                        Name = "Registration Fee",
                        Price = 8342.25M,
                        MarkUpRate = 0.05,
                        ProductId = product.Id
                    }
                };
                context.Plans.AddRange(plans);
                await context.SaveChangesAsync();
            }

            var secondProduct = await context.Products.SingleOrDefaultAsync(x => x.Name == "So fresh");
            if (secondProduct is null)
            {
                secondProduct = new Product
                {
                    Name = "So fresh",
                    SettlementAccount = "0076525143"
                };
                context.Products.Add(secondProduct);
                await context.SaveChangesAsync();

                var plans2 = new List<Plan>
                {
                    new Plan
                    {
                        Name = "Fresh Start",
                        Price = 45465.00M,
                        MarkUpRate = 0.05,
                        ProductId = secondProduct.Id
                    },
                    new Plan
                    {
                        Name = "Yummy Feast",
                        Price = 91654.50M,
                        MarkUpRate = 0.05,
                        ProductId = secondProduct.Id
                    },
                    new Plan
                    {
                        Name = "Exotic Bliss",
                        Price = 102648.00M,
                        MarkUpRate = 0.05,
                        ProductId = secondProduct.Id
                    },
                    new Plan
                    {
                        Name = "Average Meal",
                        Price = 5250.00M,
                        MarkUpRate = 0.05,
                        ProductId = secondProduct.Id
                    }
                };
                context.Plans.AddRange(plans2);
                await context.SaveChangesAsync();
            }

            var thirdProduct = await context.Products.SingleOrDefaultAsync(x => x.Name == "Healthtracka");
            if (thirdProduct is null)
            {
                thirdProduct = new Product
                {
                    Name = "Healthtracka",
                    SettlementAccount = "0076525143"
                };
                context.Products.Add(thirdProduct);
                await context.SaveChangesAsync();

                var plans3 = new List<Plan>
                {
                    new Plan
                    {
                        Name = "Bronze Package",
                        Price = 21000.00M,
                        MarkUpRate = 0.05,
                        ProductId = thirdProduct.Id
                    },
                    new Plan
                    {
                        Name = "Silver Package",
                        Price = 49350.00M,
                        MarkUpRate = 0.05,
                        ProductId = thirdProduct.Id
                    },
                    new Plan
                    {
                        Name = "Gold Package",
                        Price = 96600.00M,
                        MarkUpRate = 0.05,
                        ProductId = thirdProduct.Id
                    },
                    new Plan
                    {
                        Name = "STD Lemon",
                        Price = 21000.00M,
                        MarkUpRate = 0.05,
                        ProductId = thirdProduct.Id
                    },
                    new Plan
                    {
                        Name = "STD Lemononade",
                        Price = 39900.00M,
                        MarkUpRate = 0.05,
                        ProductId = thirdProduct.Id
                    },
                    new Plan
                    {
                        Name = "STD Lemmonade Plus",
                        Price = 77700.00M,
                        MarkUpRate = 0.05,
                        ProductId = thirdProduct.Id
                    }
                };
                context.Plans.AddRange(plans3);
                await context.SaveChangesAsync();
            }

        }
    }
}










