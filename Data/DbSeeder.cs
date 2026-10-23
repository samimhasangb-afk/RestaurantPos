using RestaurantPos.Services;

namespace RestaurantPos.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Organizations.AnyAsync()) return;
        await using var tx = await db.Database.BeginTransactionAsync();

        var org = new Organization
        {
            Name = "Saffron Hospitality",
            Phone = "01711000000",
            Email = "hello@saffron.test",
            Address = "Dhaka, Bangladesh",
            Currency = "BDT"
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var gulshan = new Branch { OrganizationId = org.Id, Name = "Gulshan", Code = "GUL", Address = "House 12, Road 55, Gulshan", Phone = "01711111111", TaxPercent = 5, ServiceChargePercent = 5 };
        var dhanmondi = new Branch { OrganizationId = org.Id, Name = "Dhanmondi", Code = "DHA", Address = "Road 27, Dhanmondi", Phone = "01712222222", TaxPercent = 5, ServiceChargePercent = 5 };
        db.Branches.AddRange(gulshan, dhanmondi);
        await db.SaveChangesAsync();

        var g1 = new Terminal { BranchId = gulshan.Id, Name = "Gulshan POS-1" };
        var g2 = new Terminal { BranchId = gulshan.Id, Name = "Gulshan POS-2" };
        var d1 = new Terminal { BranchId = dhanmondi.Id, Name = "Dhanmondi POS-1" };
        db.Terminals.AddRange(g1, g2, d1);
        await db.SaveChangesAsync();

        var defs = new (string Key, string Name, string Module)[]
        {
            ("pos.order", "Create and edit orders", "Sales"),
            ("pos.pay", "Take payment", "Sales"),
            ("pos.discount", "Apply a discount", "Sales"),
            ("pos.discount.override", "Approve large discounts", "Sales"),
            ("pos.refund", "Refund a payment", "Sales"),
            ("pos.refund.override", "Approve large refunds", "Sales"),
            ("pos.void", "Void an order", "Sales"),
            ("pos.shift", "Open and close shifts", "Sales"),
            ("kitchen.view", "View the kitchen display", "Kitchen"),
            ("kitchen.update", "Update preparation status", "Kitchen"),
            ("menu.manage", "Manage the menu", "Menu"),
            ("inventory.view", "View stock", "Inventory"),
            ("inventory.manage", "Adjust stock", "Inventory"),
            ("purchase.manage", "Purchasing and suppliers", "Inventory"),
            ("reports.view", "View reports", "Reports"),
            ("customer.manage", "Manage customers", "Sales"),
            ("expense.manage", "Record expenses", "Accounts"),
            ("delivery.manage", "Manage delivery", "Sales"),
            ("admin.users", "Manage users", "Admin"),
            ("admin.settings", "Manage settings", "Admin"),
            ("admin.backup", "Back up the database", "Admin"),
            ("approval.review", "Review approvals", "Admin"),
            ("branch.all", "Access every branch", "Admin")
        };
        var permissions = defs.Select(d => new Permission { Key = d.Key, Name = d.Name, Module = d.Module }).ToList();
        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();
        var permByKey = permissions.ToDictionary(p => p.Key);

        Role MakeRole(string name, IEnumerable<string> keys)
        {
            var role = new Role { OrganizationId = org.Id, Name = name };
            foreach (var key in keys)
                role.RolePermissions.Add(new RolePermission { Permission = permByKey[key] });
            return role;
        }

        var all = defs.Select(d => d.Key).ToArray();
        var managerKeys = all.Where(k => k is not "admin.users" and not "admin.backup").ToArray();
        var adminRole = MakeRole("Admin", all);
        var managerRole = MakeRole("Manager", managerKeys);
        var cashierRole = MakeRole("Cashier", ["pos.order", "pos.pay", "pos.discount", "pos.refund", "pos.shift", "customer.manage", "delivery.manage", "reports.view", "expense.manage", "kitchen.view"]);
        var waiterRole = MakeRole("Waiter", ["pos.order", "kitchen.view", "delivery.manage"]);
        var kitchenRole = MakeRole("Kitchen", ["kitchen.view", "kitchen.update"]);
        var storeRole = MakeRole("Storekeeper", ["inventory.view", "inventory.manage", "purchase.manage", "reports.view"]);
        db.Roles.AddRange(adminRole, managerRole, cashierRole, waiterRole, kitchenRole, storeRole);
        await db.SaveChangesAsync();

        AppUser MakeUser(string name, string username, string password, Role role, Branch home, params Branch[] access)
        {
            var user = new AppUser
            {
                OrganizationId = org.Id,
                FullName = name,
                Username = username,
                PasswordHash = PasswordUtil.Hash(password),
                RoleId = role.Id,
                DefaultBranchId = home.Id,
                Email = username + "@saffron.test"
            };
            foreach (var branch in access.Append(home).DistinctBy(b => b.Id))
                user.Branches.Add(new UserBranch { BranchId = branch.Id });
            return user;
        }

        var admin = MakeUser("Shamim Hasan", "admin", "Admin@123", adminRole, gulshan, gulshan, dhanmondi);
        var manager = MakeUser("Nadia Rahman", "manager", "Manager@123", managerRole, gulshan, gulshan, dhanmondi);
        var cashier = MakeUser("Imran Hossain", "cashier", "Cashier@123", cashierRole, gulshan);
        var waiter = MakeUser("Rafi Ahmed", "waiter", "Waiter@123", waiterRole, gulshan);
        var kitchen = MakeUser("Chef Karim", "kitchen", "Kitchen@123", kitchenRole, gulshan);
        var store = MakeUser("Lima Akter", "store", "Store@123", storeRole, gulshan);
        var cashier2 = MakeUser("Tania Sultana", "cashier2", "Cashier@123", cashierRole, dhanmondi);
        db.Users.AddRange(admin, manager, cashier, waiter, kitchen, store, cashier2);
        await db.SaveChangesAsync();

        db.SystemSettings.AddRange(
            new SystemSetting { OrganizationId = org.Id, Key = SettingKey.DiscountLimit, Value = "200" },
            new SystemSetting { OrganizationId = org.Id, Key = SettingKey.RefundLimit, Value = "300" },
            new SystemSetting { OrganizationId = org.Id, Key = SettingKey.AdjustmentLimit, Value = "20" },
            new SystemSetting { OrganizationId = org.Id, Key = SettingKey.Footer, Value = "Thank you for dining with Saffron." },
            new SystemSetting { OrganizationId = org.Id, Key = SettingKey.PublicApp, Value = "http://localhost:5173" },
            new SystemSetting { OrganizationId = org.Id, Key = SettingKey.Thermal, Value = "80" },
            new SystemSetting { OrganizationId = org.Id, Key = "smtp.host", Value = "" },
            new SystemSetting { OrganizationId = org.Id, Key = "smtp.port", Value = "587" },
            new SystemSetting { OrganizationId = org.Id, Key = "smtp.user", Value = "" },
            new SystemSetting { OrganizationId = org.Id, Key = "smtp.pass", Value = "" },
            new SystemSetting { OrganizationId = org.Id, Key = "smtp.from", Value = "receipts@saffron.test" }
        );

        var ground = new Floor { BranchId = gulshan.Id, Name = "Ground", SortOrder = 1 };
        var roof = new Floor { BranchId = gulshan.Id, Name = "Rooftop", SortOrder = 2 };
        var hall = new Floor { BranchId = dhanmondi.Id, Name = "Hall", SortOrder = 1 };
        db.Floors.AddRange(ground, roof, hall);
        await db.SaveChangesAsync();

        DiningTable[] gTables = Enumerable.Range(1, 6).Select(n => new DiningTable { BranchId = gulshan.Id, FloorId = ground.Id, Name = "G" + n, Seats = n % 2 == 0 ? 4 : 2 }).ToArray();
        DiningTable[] rTables = Enumerable.Range(1, 2).Select(n => new DiningTable { BranchId = gulshan.Id, FloorId = roof.Id, Name = "R" + n, Seats = 4 }).ToArray();
        DiningTable[] dTables = Enumerable.Range(1, 4).Select(n => new DiningTable { BranchId = dhanmondi.Id, FloorId = hall.Id, Name = "D" + n, Seats = 4 }).ToArray();
        db.DiningTables.AddRange(gTables);
        db.DiningTables.AddRange(rTables);
        db.DiningTables.AddRange(dTables);

        var catStarters = new Category { OrganizationId = org.Id, Name = "Starters", SortOrder = 1, Color = "#c9843d" };
        var catMains = new Category { OrganizationId = org.Id, Name = "Mains", SortOrder = 2, Color = "#d4652f" };
        var catGrill = new Category { OrganizationId = org.Id, Name = "Grill", SortOrder = 3, Color = "#a33b24" };
        var catDrinks = new Category { OrganizationId = org.Id, Name = "Drinks", SortOrder = 4, Color = "#2f6f4e" };
        var catDessert = new Category { OrganizationId = org.Id, Name = "Dessert", SortOrder = 5, Color = "#8a5a44" };
        db.Categories.AddRange(catStarters, catMains, catGrill, catDrinks, catDessert);
        await db.SaveChangesAsync();

        Product P(string name, Category cat, decimal price, decimal cost, string barcode, string color, int prep) => new()
        {
            OrganizationId = org.Id, CategoryId = cat.Id, Name = name, Price = price, CostPrice = cost,
            Barcode = barcode, Sku = barcode, Color = color, PrepMinutes = prep
        };

        var products = new List<Product>
        {
            P("Chicken Pakora", catStarters, 180, 70, "1001001", "#f3d7b0", 12),
            P("Shingara", catStarters, 40, 12, "1001002", "#f6e2c2", 8),
            P("Vegetable Soup", catStarters, 160, 50, "1001003", "#e7d7a1", 10),
            P("Chicken Biryani", catMains, 320, 140, "1002001", "#e7c48a", 25),
            P("Kacchi Biryani", catMains, 450, 210, "1002002", "#e0b15a", 30),
            P("Beef Tehari", catMains, 350, 160, "1002003", "#d7a15a", 25),
            P("Chicken Curry", catMains, 260, 110, "1002004", "#e39a62", 20),
            P("Shorshe Ilish", catMains, 520, 280, "1002005", "#d9c07a", 22),
            P("Dal Tadka", catMains, 90, 25, "1002006", "#e6c56e", 12),
            P("Steamed Rice", catMains, 60, 15, "1002007", "#f4efe4", 10),
            P("Veg Thali", catMains, 240, 90, "1002008", "#d5c48a", 18),
            P("Grill Chicken", catGrill, 380, 170, "1003001", "#c96b45", 22),
            P("Beef Kebab", catGrill, 300, 140, "1003002", "#b85a38", 18),
            P("French Fries", catGrill, 120, 40, "1003003", "#f0d48a", 8),
            P("Borhani", catDrinks, 90, 30, "1004001", "#efe6c9", 5),
            P("Mango Lassi", catDrinks, 80, 28, "1004002", "#f3d27a", 5),
            P("Coffee", catDrinks, 150, 40, "1004003", "#c4a484", 6),
            P("Mineral Water", catDrinks, 20, 8, "1004004", "#d9e7ef", 1),
            P("Firni", catDessert, 90, 30, "1005001", "#f6efe4", 5),
            P("Mishti Doi", catDessert, 70, 25, "1005002", "#f3e0c4", 2)
        };
        db.Products.AddRange(products);

        var spice = new ModifierGroup { OrganizationId = org.Id, Name = "Spice level", Required = true, MinSelect = 1, MaxSelect = 1 };
        spice.Options.Add(new ModifierOption { Name = "Mild", Price = 0 });
        spice.Options.Add(new ModifierOption { Name = "Medium", Price = 0 });
        spice.Options.Add(new ModifierOption { Name = "Hot", Price = 0 });
        var size = new ModifierGroup { OrganizationId = org.Id, Name = "Size", Required = false, MinSelect = 0, MaxSelect = 1 };
        size.Options.Add(new ModifierOption { Name = "Regular", Price = 0 });
        size.Options.Add(new ModifierOption { Name = "Large", Price = 80 });
        db.ModifierGroups.AddRange(spice, size);

        var egg = new Addon { OrganizationId = org.Id, Name = "Fried egg", Price = 30 };
        var raita = new Addon { OrganizationId = org.Id, Name = "Raita", Price = 25 };
        var extraRice = new Addon { OrganizationId = org.Id, Name = "Extra rice", Price = 40 };
        db.Addons.AddRange(egg, raita, extraRice);
        await db.SaveChangesAsync();

        var byName = products.ToDictionary(p => p.Name);
        string[] spicy = ["Chicken Biryani", "Kacchi Biryani", "Beef Tehari", "Chicken Curry", "Shorshe Ilish", "Grill Chicken", "Beef Kebab"];
        foreach (var name in spicy)
            db.ProductModifierGroups.Add(new ProductModifierGroup { ProductId = byName[name].Id, ModifierGroupId = spice.Id });
        foreach (var name in new[] { "Borhani", "Mango Lassi", "Coffee", "Grill Chicken" })
            db.ProductModifierGroups.Add(new ProductModifierGroup { ProductId = byName[name].Id, ModifierGroupId = size.Id });
        foreach (var name in new[] { "Chicken Biryani", "Kacchi Biryani", "Beef Tehari", "Chicken Curry", "Veg Thali", "Grill Chicken" })
        {
            db.ProductAddons.Add(new ProductAddon { ProductId = byName[name].Id, AddonId = egg.Id });
            db.ProductAddons.Add(new ProductAddon { ProductId = byName[name].Id, AddonId = raita.Id });
            db.ProductAddons.Add(new ProductAddon { ProductId = byName[name].Id, AddonId = extraRice.Id });
        }

        db.Coupons.Add(new Coupon { OrganizationId = org.Id, Code = "WELCOME10", DiscountType = "Percent", Value = 10, MinOrder = 200, MaxDiscount = 150 });
        db.Coupons.Add(new Coupon { OrganizationId = org.Id, Code = "LUNCH50", DiscountType = "Fixed", Value = 50, MinOrder = 300, MaxDiscount = 50 });

        var rahim = new Customer { OrganizationId = org.Id, Name = "Rahim Uddin", Phone = "01819000001", Address = "Banani" };
        var ayesha = new Customer { OrganizationId = org.Id, Name = "Ayesha Khan", Phone = "01819000002", Email = "ayesha@example.com", DueBalance = 150, Address = "Dhanmondi" };
        db.Customers.AddRange(rahim, ayesha);

        Ingredient I(string name, string unit, decimal reorder, decimal cost, bool expiry = false) => new()
        {
            OrganizationId = org.Id, Name = name, Unit = unit, ReorderLevel = reorder, CostPerUnit = cost, ExpiryTracked = expiry, Sku = name.Replace(" ", "").ToUpperInvariant()
        };
        var rice = I("Rice", "kg", 5, 70);
        var chicken = I("Chicken", "kg", 3, 320);
        var mutton = I("Mutton", "kg", 2, 980);
        var beef = I("Beef", "kg", 2, 720);
        var hilsa = I("Hilsa", "kg", 2, 900, true);
        var onion = I("Onion", "kg", 3, 60);
        var potato = I("Potato", "kg", 4, 40);
        var yogurt = I("Yogurt", "kg", 2, 180, true);
        var milk = I("Milk", "L", 3, 90, true);
        var oil = I("Cooking oil", "L", 2, 180);
        var spiceMix = I("Spice mix", "kg", 0.4m, 800);
        var flour = I("Flour", "kg", 2, 55);
        var sugar = I("Sugar", "kg", 2, 110);
        var veg = I("Vegetable mix", "kg", 2, 70, true);
        var beans = I("Coffee beans", "kg", 0.2m, 1400);
        var eggIng = I("Egg", "pcs", 12, 14);
        var lentil = I("Lentil", "kg", 2, 140);
        var ingredients = new[] { rice, chicken, mutton, beef, hilsa, onion, potato, yogurt, milk, oil, spiceMix, flour, sugar, veg, beans, eggIng, lentil };
        db.Ingredients.AddRange(ingredients);
        await db.SaveChangesAsync();

        void Recipe(string product, params (Ingredient Item, decimal Qty)[] lines)
        {
            foreach (var line in lines)
                db.RecipeLines.Add(new RecipeLine { ProductId = byName[product].Id, IngredientId = line.Item.Id, Quantity = line.Qty });
        }
        Recipe("Chicken Biryani", (rice, 0.25m), (chicken, 0.18m), (onion, 0.05m), (yogurt, 0.04m), (oil, 0.02m), (spiceMix, 0.015m));
        Recipe("Kacchi Biryani", (rice, 0.28m), (mutton, 0.20m), (yogurt, 0.05m), (onion, 0.05m), (oil, 0.02m), (spiceMix, 0.02m));
        Recipe("Beef Tehari", (rice, 0.25m), (beef, 0.16m), (onion, 0.05m), (oil, 0.02m), (spiceMix, 0.015m));
        Recipe("Chicken Curry", (chicken, 0.20m), (onion, 0.06m), (oil, 0.02m), (spiceMix, 0.012m));
        Recipe("Shorshe Ilish", (hilsa, 0.25m), (oil, 0.03m), (spiceMix, 0.01m));
        Recipe("Dal Tadka", (lentil, 0.08m), (onion, 0.02m), (oil, 0.01m), (spiceMix, 0.008m));
        Recipe("Steamed Rice", (rice, 0.20m));
        Recipe("Veg Thali", (rice, 0.15m), (veg, 0.12m), (lentil, 0.05m), (oil, 0.015m));
        Recipe("Chicken Pakora", (chicken, 0.08m), (flour, 0.04m), (oil, 0.03m));
        Recipe("Shingara", (flour, 0.04m), (potato, 0.05m), (oil, 0.02m));
        Recipe("Vegetable Soup", (veg, 0.10m), (milk, 0.05m));
        Recipe("Grill Chicken", (chicken, 0.28m), (spiceMix, 0.01m), (oil, 0.015m));
        Recipe("Beef Kebab", (beef, 0.18m), (spiceMix, 0.01m), (onion, 0.03m));
        Recipe("French Fries", (potato, 0.20m), (oil, 0.03m));
        Recipe("Borhani", (yogurt, 0.18m), (milk, 0.05m));
        Recipe("Mango Lassi", (yogurt, 0.15m), (milk, 0.08m), (sugar, 0.02m));
        Recipe("Coffee", (beans, 0.018m), (milk, 0.05m), (sugar, 0.01m));
        Recipe("Firni", (milk, 0.20m), (rice, 0.03m), (sugar, 0.02m));
        Recipe("Mishti Doi", (milk, 0.20m), (sugar, 0.03m), (yogurt, 0.02m));

        void Stock(Branch branch, Ingredient ingredient, decimal qty, DateTime? expiry = null)
        {
            db.StockBalances.Add(new StockBalance { BranchId = branch.Id, IngredientId = ingredient.Id, Quantity = qty, AvgCost = ingredient.CostPerUnit, ExpiryDate = expiry });
            db.StockLedgers.Add(new StockLedger
            {
                BranchId = branch.Id, IngredientId = ingredient.Id, TxnType = "Opening", Quantity = qty,
                UnitCost = ingredient.CostPerUnit, BalanceAfter = qty, Note = "Opening stock", UserId = admin.Id
            });
        }
        foreach (var branch in new[] { gulshan, dhanmondi })
        {
            var factor = branch.Id == gulshan.Id ? 1m : 0.6m;
            Stock(branch, rice, 40 * factor);
            Stock(branch, chicken, 18 * factor);
            Stock(branch, mutton, 8 * factor);
            Stock(branch, beef, 10 * factor);
            Stock(branch, hilsa, branch.Id == gulshan.Id ? 6 : 1, DateTime.UtcNow.Date.AddDays(3));
            Stock(branch, onion, 12 * factor);
            Stock(branch, potato, 16 * factor);
            Stock(branch, yogurt, 6 * factor, DateTime.UtcNow.Date.AddDays(5));
            Stock(branch, milk, 8 * factor, DateTime.UtcNow.Date.AddDays(12));
            Stock(branch, oil, 10 * factor);
            Stock(branch, spiceMix, 2 * factor);
            Stock(branch, flour, 8 * factor);
            Stock(branch, sugar, 6 * factor);
            Stock(branch, veg, 7 * factor, DateTime.UtcNow.Date.AddDays(2));
            Stock(branch, beans, 1 * factor);
            Stock(branch, eggIng, 48 * factor);
            Stock(branch, lentil, 8 * factor);
        }

        var fresh = new Supplier { OrganizationId = org.Id, Name = "Karwan Bazar Fresh", Phone = "01911000001", Address = "Karwan Bazar", DueBalance = 8500 };
        var meat = new Supplier { OrganizationId = org.Id, Name = "Meat House", Phone = "01911000002", Address = "Tejgaon", DueBalance = 4200 };
        db.Suppliers.AddRange(fresh, meat);
        await db.SaveChangesAsync();

        var po = new PurchaseOrder
        {
            OrganizationId = org.Id, BranchId = gulshan.Id, SupplierId = fresh.Id,
            PoNo = await Sequences.Next(db, org.Id, "po", "PO-"), Status = "Open", UserId = store.Id,
            ExpectedDate = DateTime.UtcNow.AddDays(2), Notes = "Weekly dry stock"
        };
        po.Lines.Add(new PurchaseOrderLine { IngredientId = rice.Id, Quantity = 25, UnitCost = 70 });
        po.Lines.Add(new PurchaseOrderLine { IngredientId = lentil.Id, Quantity = 10, UnitCost = 140 });
        po.Total = 25 * 70 + 10 * 140;
        db.PurchaseOrders.Add(po);

        db.Expenses.Add(new Expense { OrganizationId = org.Id, BranchId = gulshan.Id, Category = "Gas", Amount = 1800, Note = "Cylinder", UserId = cashier.Id, SpentAt = Clock.ToUtcFromBd(DateTime.Now.AddDays(-1)) });
        db.Expenses.Add(new Expense { OrganizationId = org.Id, BranchId = gulshan.Id, Category = "Cleaning", Amount = 600, Note = "Kitchen cleaning", UserId = cashier.Id, SpentAt = Clock.ToUtcFromBd(DateTime.Now.AddDays(-2)) });
        db.Expenses.Add(new Expense { OrganizationId = org.Id, BranchId = dhanmondi.Id, Category = "Utilities", Amount = 2400, Note = "Electricity", UserId = cashier2.Id, SpentAt = Clock.ToUtcFromBd(DateTime.Now.AddDays(-1)) });
        db.Wastages.Add(new Wastage { BranchId = gulshan.Id, IngredientId = veg.Id, Quantity = 0.4m, Cost = Money.R(0.4m * veg.CostPerUnit), Reason = "Wilted vegetables", UserId = store.Id, CreatedAt = DateTime.UtcNow.AddDays(-1) });

        async Task Paid(Branch branch, Terminal terminal, AppUser who, AppUser? server, string type, DateTime when, string method, params (string Name, decimal Qty)[] items)
        {
            var order = new SaleOrder
            {
                OrganizationId = org.Id,
                BranchId = branch.Id,
                TerminalId = terminal.Id,
                OrderNo = await Sequences.Next(db, org.Id, "order-" + branch.Id, branch.Code + "-"),
                OrderType = type,
                Status = "Open",
                WaiterId = server?.Id,
                CashierId = who.Id,
                CreatedAt = when,
                PaidAt = when,
                DeliveryStatus = type is "Delivery" or "Online" ? "Delivered" : "None"
            };
            foreach (var item in items)
            {
                var product = byName[item.Name];
                order.Lines.Add(new SaleLine
                {
                    ProductId = product.Id, Name = product.Name, Qty = item.Qty, UnitPrice = product.Price,
                    Course = product.CategoryId == catDrinks.Id ? "Drink" : product.CategoryId == catDessert.Id ? "Dessert" : product.CategoryId == catStarters.Id ? "Starter" : "Main",
                    PrepStatus = "Served", SentToKitchen = true, StockConsumed = true
                });
            }
            Pricing.Apply(order, branch, null);
            foreach (var line in order.Lines) line.FoodCost = Money.R(line.LineTotal * 0.38m);
            order.FoodCost = Money.R(order.Lines.Sum(l => l.FoodCost));
            order.PaidTotal = order.GrandTotal;
            Pricing.RefreshPayment(order);
            Pricing.RefreshKitchen(order);
            order.Tenders.Add(new Tender { Method = method, Amount = order.GrandTotal, Tendered = order.GrandTotal, UserId = who.Id, CreatedAt = when });
            order.Receipts.Add(new Receipt
            {
                ReceiptNo = await Sequences.Next(db, org.Id, "receipt", "RCP-"),
                Token = Guid.NewGuid().ToString("N"),
                PrintCount = 1,
                CreatedAt = when
            });
            db.SaleOrders.Add(order);
            await db.SaveChangesAsync();
        }

        var today = DateTime.Now;
        for (var day = 5; day >= 0; day--)
        {
            var lunch = Clock.ToUtcFromBd(today.Date.AddDays(-day).AddHours(13));
            var dinner = Clock.ToUtcFromBd(today.Date.AddDays(-day).AddHours(20));
            await Paid(gulshan, g1, cashier, waiter, "DineIn", lunch, day % 2 == 0 ? "Cash" : "Card", ("Chicken Biryani", 2), ("Borhani", 2));
            await Paid(gulshan, g1, cashier, waiter, "Takeaway", dinner, "Mobile", ("Beef Kebab", 2), ("French Fries", 1), ("Coffee", 1));
            await Paid(dhanmondi, d1, cashier2, null, "DineIn", lunch.AddMinutes(20), "Cash", ("Kacchi Biryani", 1), ("Mineral Water", 2));
        }
        await Paid(gulshan, g1, cashier, waiter, "Delivery", Clock.ToUtcFromBd(today.Date.AddHours(19)), "Mobile", ("Chicken Curry", 1), ("Steamed Rice", 2));

        var open = new SaleOrder
        {
            OrganizationId = org.Id, BranchId = gulshan.Id, TerminalId = g1.Id, TableId = gTables[0].Id,
            OrderNo = await Sequences.Next(db, org.Id, "order-" + gulshan.Id, "GUL-"),
            OrderType = "DineIn", WaiterId = waiter.Id, CashierId = cashier.Id, Status = "Open"
        };
        open.Lines.Add(new SaleLine { ProductId = byName["Chicken Biryani"].Id, Name = "Chicken Biryani", Qty = 2, UnitPrice = 320, Course = "Main" });
        open.Lines[0].Extras.Add(new SaleLineExtra { Kind = "Modifier", Name = "Hot", Price = 0 });
        open.Lines.Add(new SaleLine { ProductId = byName["Borhani"].Id, Name = "Borhani", Qty = 2, UnitPrice = 90, Course = "Drink" });
        Pricing.Apply(open, gulshan, null);
        Pricing.RefreshKitchen(open);
        db.SaleOrders.Add(open);
        gTables[0].Status = "Occupied";

        var held = new SaleOrder
        {
            OrganizationId = org.Id, BranchId = gulshan.Id, TerminalId = g1.Id,
            OrderNo = await Sequences.Next(db, org.Id, "order-" + gulshan.Id, "GUL-"),
            OrderType = "Takeaway", Status = "Held", CashierId = cashier.Id, Notes = "Customer stepped outside"
        };
        held.Lines.Add(new SaleLine { ProductId = byName["French Fries"].Id, Name = "French Fries", Qty = 1, UnitPrice = 120, Course = "Starter" });
        Pricing.Apply(held, gulshan, null);
        db.SaleOrders.Add(held);

        var delivery = new SaleOrder
        {
            OrganizationId = org.Id, BranchId = gulshan.Id, TerminalId = g1.Id,
            OrderNo = await Sequences.Next(db, org.Id, "order-" + gulshan.Id, "GUL-"),
            OrderType = "Online", Status = "Open", DeliveryStatus = "Assigned", RiderId = waiter.Id,
            CustomerId = rahim.Id, DeliveryAddress = "Road 11, Banani", DeliveryPhone = "01819000001", KitchenStatus = "Preparing"
        };
        var dLine = new SaleLine
        {
            ProductId = byName["Grill Chicken"].Id, Name = "Grill Chicken", Qty = 1, UnitPrice = 380, Course = "Main",
            SentToKitchen = true, PrepStatus = "Preparing", StockConsumed = true, FoodCost = 144
        };
        delivery.Lines.Add(dLine);
        Pricing.Apply(delivery, gulshan, null);
        delivery.FoodCost = 144;
        db.SaleOrders.Add(delivery);
        db.KitchenTickets.Add(new KitchenTicket
        {
            SaleOrder = delivery,
            TicketNo = await Sequences.Next(db, org.Id, "kot", "KOT-"),
            Course = "Main",
            Status = "Preparing",
            Lines = { new KitchenTicketLine { SourceLineId = 0, Name = "Grill Chicken", Qty = 1, PrepStatus = "Preparing", Notes = "Online" } }
        });

        db.Shifts.Add(new Shift { BranchId = gulshan.Id, TerminalId = g1.Id, UserId = cashier.Id, OpeningCash = 5000, Status = "Open" });
        await db.SaveChangesAsync();

        var ticketLine = await db.KitchenTicketLines.OrderByDescending(l => l.Id).FirstAsync();
        ticketLine.SourceLineId = dLine.Id;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
