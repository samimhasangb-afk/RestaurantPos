/*
    Saffron POS — SQL Server schema
    Database: RestaurantPosDb
    Server:   DESKTOP-E46P7NN\SQLEXPRESS

    The API creates this same schema on first run (EF Core EnsureCreated)
    and then seeds demo data. Run this script only on an empty database
    when you want the tables without starting the API.
*/

IF DB_ID(N'RestaurantPosDb') IS NULL
    CREATE DATABASE RestaurantPosDb;
GO
USE RestaurantPosDb;
GO

CREATE TABLE dbo.Organizations (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Organizations PRIMARY KEY,
    Name nvarchar(160) NOT NULL,
    Phone nvarchar(40) NULL,
    Email nvarchar(160) NULL,
    Address nvarchar(2000) NULL,
    Currency nvarchar(80) NOT NULL,
    IsActive bit NOT NULL,
    CreatedAt datetime2 NOT NULL
);

CREATE TABLE dbo.Permissions (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY,
    [Key] nvarchar(60) NOT NULL,
    Name nvarchar(160) NOT NULL,
    Module nvarchar(80) NOT NULL,
    CONSTRAINT UQ_Permissions_Key UNIQUE ([Key])
);

CREATE TABLE dbo.Branches (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Branches PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Code nvarchar(60) NOT NULL,
    Address nvarchar(2000) NULL,
    Phone nvarchar(40) NULL,
    TaxPercent decimal(18,2) NOT NULL,
    ServiceChargePercent decimal(18,2) NOT NULL,
    IsActive bit NOT NULL,
    CONSTRAINT FK_Branches_Organizations FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id)
);

CREATE TABLE dbo.Roles (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    CONSTRAINT FK_Roles_Organizations FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id),
    CONSTRAINT UQ_Roles_Organization_Name UNIQUE (OrganizationId, Name)
);

CREATE TABLE dbo.Terminals (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Terminals PRIMARY KEY,
    BranchId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    IsActive bit NOT NULL,
    CONSTRAINT FK_Terminals_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id)
);

CREATE TABLE dbo.Users (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    OrganizationId int NOT NULL,
    FullName nvarchar(160) NOT NULL,
    Username nvarchar(60) NOT NULL,
    PasswordHash nvarchar(500) NOT NULL,
    Email nvarchar(160) NULL,
    Phone nvarchar(40) NULL,
    RoleId int NOT NULL,
    DefaultBranchId int NULL,
    IsActive bit NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT FK_Users_Organizations FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
    CONSTRAINT FK_Users_Branches_DefaultBranch FOREIGN KEY (DefaultBranchId) REFERENCES dbo.Branches(Id)
);

CREATE TABLE dbo.RolePermissions (
    RoleId int NOT NULL,
    PermissionId int NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id)
);

CREATE TABLE dbo.UserBranches (
    UserId int NOT NULL,
    BranchId int NOT NULL,
    CONSTRAINT PK_UserBranches PRIMARY KEY (UserId, BranchId),
    CONSTRAINT FK_UserBranches_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_UserBranches_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id)
);

CREATE TABLE dbo.Floors (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Floors PRIMARY KEY,
    BranchId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    SortOrder int NOT NULL,
    CONSTRAINT FK_Floors_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id)
);

CREATE TABLE dbo.DiningTables (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DiningTables PRIMARY KEY,
    BranchId int NOT NULL,
    FloorId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Seats int NOT NULL,
    Status nvarchar(80) NOT NULL,
    CONSTRAINT FK_DiningTables_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
    CONSTRAINT FK_DiningTables_Floors FOREIGN KEY (FloorId) REFERENCES dbo.Floors(Id)
);

CREATE TABLE dbo.Categories (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    SortOrder int NOT NULL,
    Color nvarchar(80) NOT NULL,
    IsActive bit NOT NULL
);

CREATE TABLE dbo.Products (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    OrganizationId int NOT NULL,
    CategoryId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Sku nvarchar(60) NULL,
    Barcode nvarchar(60) NULL,
    Price decimal(18,2) NOT NULL,
    CostPrice decimal(18,2) NOT NULL,
    PrepMinutes int NOT NULL,
    Color nvarchar(80) NOT NULL,
    IsActive bit NOT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id)
);
CREATE UNIQUE INDEX UQ_Products_Organization_Barcode ON dbo.Products (OrganizationId, Barcode) WHERE Barcode IS NOT NULL;

CREATE TABLE dbo.ModifierGroups (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModifierGroups PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Required bit NOT NULL,
    MinSelect int NOT NULL,
    MaxSelect int NOT NULL
);

CREATE TABLE dbo.ModifierOptions (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModifierOptions PRIMARY KEY,
    ModifierGroupId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Price decimal(18,2) NOT NULL,
    IsActive bit NOT NULL,
    CONSTRAINT FK_ModifierOptions_ModifierGroups FOREIGN KEY (ModifierGroupId) REFERENCES dbo.ModifierGroups(Id)
);

CREATE TABLE dbo.ProductModifierGroups (
    ProductId int NOT NULL,
    ModifierGroupId int NOT NULL,
    CONSTRAINT PK_ProductModifierGroups PRIMARY KEY (ProductId, ModifierGroupId),
    CONSTRAINT FK_ProductModifierGroups_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT FK_ProductModifierGroups_ModifierGroups FOREIGN KEY (ModifierGroupId) REFERENCES dbo.ModifierGroups(Id)
);

CREATE TABLE dbo.Addons (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Addons PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Price decimal(18,2) NOT NULL,
    IsActive bit NOT NULL
);

CREATE TABLE dbo.ProductAddons (
    ProductId int NOT NULL,
    AddonId int NOT NULL,
    CONSTRAINT PK_ProductAddons PRIMARY KEY (ProductId, AddonId),
    CONSTRAINT FK_ProductAddons_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT FK_ProductAddons_Addons FOREIGN KEY (AddonId) REFERENCES dbo.Addons(Id)
);

CREATE TABLE dbo.Coupons (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Coupons PRIMARY KEY,
    OrganizationId int NOT NULL,
    Code nvarchar(60) NOT NULL,
    DiscountType nvarchar(80) NOT NULL,
    Value decimal(18,2) NOT NULL,
    MinOrder decimal(18,2) NOT NULL,
    MaxDiscount decimal(18,2) NOT NULL,
    ExpiresOn datetime2 NULL,
    IsActive bit NOT NULL,
    CONSTRAINT UQ_Coupons_Organization_Code UNIQUE (OrganizationId, Code)
);

CREATE TABLE dbo.Customers (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Phone nvarchar(40) NULL,
    Email nvarchar(160) NULL,
    Address nvarchar(2000) NULL,
    DueBalance decimal(18,2) NOT NULL,
    IsActive bit NOT NULL
);

CREATE TABLE dbo.DuePayments (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DuePayments PRIMARY KEY,
    CustomerId int NOT NULL,
    Amount decimal(18,2) NOT NULL,
    Method nvarchar(80) NOT NULL,
    Note nvarchar(2000) NULL,
    UserId int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_DuePayments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
);

CREATE TABLE dbo.Shifts (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shifts PRIMARY KEY,
    BranchId int NOT NULL,
    TerminalId int NOT NULL,
    UserId int NOT NULL,
    OpenedAt datetime2 NOT NULL,
    ClosedAt datetime2 NULL,
    OpeningCash decimal(18,2) NOT NULL,
    ClosingCash decimal(18,2) NOT NULL,
    ExpectedCash decimal(18,2) NOT NULL,
    Status nvarchar(80) NOT NULL,
    Note nvarchar(2000) NULL,
    CONSTRAINT FK_Shifts_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
    CONSTRAINT FK_Shifts_Terminals FOREIGN KEY (TerminalId) REFERENCES dbo.Terminals(Id),
    CONSTRAINT FK_Shifts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);

CREATE TABLE dbo.SaleOrders (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleOrders PRIMARY KEY,
    OrganizationId int NOT NULL,
    BranchId int NOT NULL,
    TerminalId int NULL,
    ShiftId int NULL,
    OrderNo nvarchar(60) NOT NULL,
    OrderType nvarchar(80) NOT NULL,
    Status nvarchar(80) NOT NULL,
    KitchenStatus nvarchar(80) NOT NULL,
    PaymentStatus nvarchar(80) NOT NULL,
    TableId int NULL,
    CustomerId int NULL,
    WaiterId int NULL,
    CashierId int NULL,
    RiderId int NULL,
    CouponCode nvarchar(80) NULL,
    Notes nvarchar(2000) NULL,
    DeliveryAddress nvarchar(2000) NULL,
    DeliveryPhone nvarchar(40) NULL,
    DeliveryStatus nvarchar(80) NOT NULL,
    Subtotal decimal(18,2) NOT NULL,
    ManualDiscount decimal(18,2) NOT NULL,
    DiscountTotal decimal(18,2) NOT NULL,
    TaxTotal decimal(18,2) NOT NULL,
    ServiceCharge decimal(18,2) NOT NULL,
    GrandTotal decimal(18,2) NOT NULL,
    PaidTotal decimal(18,2) NOT NULL,
    RefundedTotal decimal(18,2) NOT NULL,
    FoodCost decimal(18,2) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    PaidAt datetime2 NULL,
    CONSTRAINT UQ_SaleOrders_OrderNo UNIQUE (OrderNo),
    CONSTRAINT FK_SaleOrders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
    CONSTRAINT FK_SaleOrders_DiningTables FOREIGN KEY (TableId) REFERENCES dbo.DiningTables(Id),
    CONSTRAINT FK_SaleOrders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
    CONSTRAINT FK_SaleOrders_Users_Waiter FOREIGN KEY (WaiterId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_SaleOrders_Users_Cashier FOREIGN KEY (CashierId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_SaleOrders_Users_Rider FOREIGN KEY (RiderId) REFERENCES dbo.Users(Id)
);
CREATE INDEX IX_SaleOrders_Branch_CreatedAt ON dbo.SaleOrders (BranchId, CreatedAt);

CREATE TABLE dbo.SaleLines (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleLines PRIMARY KEY,
    SaleOrderId int NOT NULL,
    ProductId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Qty decimal(18,3) NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    DiscountAmount decimal(18,2) NOT NULL,
    FoodCost decimal(18,2) NOT NULL,
    Course nvarchar(80) NOT NULL,
    Notes nvarchar(2000) NULL,
    PrepStatus nvarchar(80) NOT NULL,
    IsVoid bit NOT NULL,
    SentToKitchen bit NOT NULL,
    StockConsumed bit NOT NULL,
    LineTotal decimal(18,2) NOT NULL,
    CONSTRAINT FK_SaleLines_SaleOrders FOREIGN KEY (SaleOrderId) REFERENCES dbo.SaleOrders(Id),
    CONSTRAINT FK_SaleLines_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
);

CREATE TABLE dbo.SaleLineExtras (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleLineExtras PRIMARY KEY,
    SaleLineId int NOT NULL,
    Kind nvarchar(80) NOT NULL,
    Name nvarchar(160) NOT NULL,
    Price decimal(18,2) NOT NULL,
    CONSTRAINT FK_SaleLineExtras_SaleLines FOREIGN KEY (SaleLineId) REFERENCES dbo.SaleLines(Id)
);

CREATE TABLE dbo.Tenders (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tenders PRIMARY KEY,
    SaleOrderId int NOT NULL,
    Method nvarchar(80) NOT NULL,
    Amount decimal(18,2) NOT NULL,
    Tendered decimal(18,2) NOT NULL,
    ChangeDue decimal(18,2) NOT NULL,
    Reference nvarchar(80) NULL,
    IsRefund bit NOT NULL,
    UserId int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_Tenders_SaleOrders FOREIGN KEY (SaleOrderId) REFERENCES dbo.SaleOrders(Id)
);

CREATE TABLE dbo.Receipts (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Receipts PRIMARY KEY,
    SaleOrderId int NOT NULL,
    ReceiptNo nvarchar(60) NOT NULL,
    Token nvarchar(60) NOT NULL,
    PrintCount int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT UQ_Receipts_ReceiptNo UNIQUE (ReceiptNo),
    CONSTRAINT UQ_Receipts_Token UNIQUE (Token),
    CONSTRAINT FK_Receipts_SaleOrders FOREIGN KEY (SaleOrderId) REFERENCES dbo.SaleOrders(Id)
);

CREATE TABLE dbo.ReceiptEvents (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceiptEvents PRIMARY KEY,
    ReceiptId int NOT NULL,
    Action nvarchar(80) NOT NULL,
    Note nvarchar(2000) NULL,
    UserId int NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_ReceiptEvents_Receipts FOREIGN KEY (ReceiptId) REFERENCES dbo.Receipts(Id)
);

CREATE TABLE dbo.ReceiptDispatches (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceiptDispatches PRIMARY KEY,
    ReceiptId int NOT NULL,
    Channel nvarchar(80) NOT NULL,
    Destination nvarchar(80) NOT NULL,
    Status nvarchar(80) NOT NULL,
    Note nvarchar(2000) NULL,
    UserId int NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_ReceiptDispatches_Receipts FOREIGN KEY (ReceiptId) REFERENCES dbo.Receipts(Id)
);

CREATE TABLE dbo.KitchenTickets (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_KitchenTickets PRIMARY KEY,
    SaleOrderId int NOT NULL,
    TicketNo nvarchar(60) NOT NULL,
    Course nvarchar(80) NOT NULL,
    Status nvarchar(80) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_KitchenTickets_SaleOrders FOREIGN KEY (SaleOrderId) REFERENCES dbo.SaleOrders(Id)
);

CREATE TABLE dbo.KitchenTicketLines (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_KitchenTicketLines PRIMARY KEY,
    KitchenTicketId int NOT NULL,
    SourceLineId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Qty decimal(18,3) NOT NULL,
    Notes nvarchar(2000) NULL,
    PrepStatus nvarchar(80) NOT NULL,
    CONSTRAINT FK_KitchenTicketLines_KitchenTickets FOREIGN KEY (KitchenTicketId) REFERENCES dbo.KitchenTickets(Id)
);

CREATE TABLE dbo.NumberSequences (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_NumberSequences PRIMARY KEY,
    OrganizationId int NOT NULL,
    [Key] nvarchar(60) NOT NULL,
    LastValue int NOT NULL,
    CONSTRAINT UQ_NumberSequences_Organization_Key UNIQUE (OrganizationId, [Key])
);

CREATE TABLE dbo.Ingredients (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ingredients PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Sku nvarchar(60) NULL,
    Unit nvarchar(80) NOT NULL,
    ReorderLevel decimal(18,3) NOT NULL,
    CostPerUnit decimal(18,2) NOT NULL,
    ExpiryTracked bit NOT NULL,
    IsActive bit NOT NULL
);

CREATE TABLE dbo.RecipeLines (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecipeLines PRIMARY KEY,
    ProductId int NOT NULL,
    IngredientId int NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    CONSTRAINT FK_RecipeLines_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT FK_RecipeLines_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id)
);

CREATE TABLE dbo.StockBalances (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockBalances PRIMARY KEY,
    BranchId int NOT NULL,
    IngredientId int NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    AvgCost decimal(18,2) NOT NULL,
    ExpiryDate datetime2 NULL,
    CONSTRAINT UQ_StockBalances_Branch_Ingredient UNIQUE (BranchId, IngredientId),
    CONSTRAINT FK_StockBalances_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
    CONSTRAINT FK_StockBalances_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id)
);

CREATE TABLE dbo.StockLedgers (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockLedgers PRIMARY KEY,
    BranchId int NOT NULL,
    IngredientId int NOT NULL,
    TxnType nvarchar(80) NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    UnitCost decimal(18,2) NOT NULL,
    BalanceAfter decimal(18,2) NOT NULL,
    RefType nvarchar(80) NULL,
    RefId int NULL,
    Note nvarchar(2000) NULL,
    UserId int NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_StockLedgers_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id)
);

CREATE TABLE dbo.Suppliers (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    OrganizationId int NOT NULL,
    Name nvarchar(160) NOT NULL,
    Phone nvarchar(40) NULL,
    Email nvarchar(160) NULL,
    Address nvarchar(2000) NULL,
    DueBalance decimal(18,2) NOT NULL,
    IsActive bit NOT NULL
);

CREATE TABLE dbo.SupplierPayments (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPayments PRIMARY KEY,
    SupplierId int NOT NULL,
    Amount decimal(18,2) NOT NULL,
    Method nvarchar(80) NOT NULL,
    Note nvarchar(2000) NULL,
    UserId int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_SupplierPayments_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id)
);

CREATE TABLE dbo.PurchaseOrders (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrders PRIMARY KEY,
    OrganizationId int NOT NULL,
    BranchId int NOT NULL,
    SupplierId int NOT NULL,
    PoNo nvarchar(60) NOT NULL,
    Status nvarchar(80) NOT NULL,
    OrderDate datetime2 NOT NULL,
    ExpectedDate datetime2 NULL,
    Total decimal(18,2) NOT NULL,
    Notes nvarchar(2000) NULL,
    UserId int NOT NULL,
    CONSTRAINT FK_PurchaseOrders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
    CONSTRAINT FK_PurchaseOrders_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id)
);

CREATE TABLE dbo.PurchaseOrderLines (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrderLines PRIMARY KEY,
    PurchaseOrderId int NOT NULL,
    IngredientId int NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    UnitCost decimal(18,2) NOT NULL,
    ReceivedQty decimal(18,3) NOT NULL,
    CONSTRAINT FK_PurchaseOrderLines_PurchaseOrders FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrders(Id),
    CONSTRAINT FK_PurchaseOrderLines_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id)
);

CREATE TABLE dbo.GoodsReceipts (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GoodsReceipts PRIMARY KEY,
    PurchaseOrderId int NOT NULL,
    GrnNo nvarchar(60) NOT NULL,
    ReceivedAt datetime2 NOT NULL,
    UserId int NOT NULL,
    Note nvarchar(2000) NULL,
    Total decimal(18,2) NOT NULL,
    CONSTRAINT FK_GoodsReceipts_PurchaseOrders FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrders(Id)
);

CREATE TABLE dbo.GoodsReceiptLines (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GoodsReceiptLines PRIMARY KEY,
    GoodsReceiptId int NOT NULL,
    PurchaseOrderLineId int NOT NULL,
    IngredientId int NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    UnitCost decimal(18,2) NOT NULL,
    ExpiryDate datetime2 NULL,
    CONSTRAINT FK_GoodsReceiptLines_GoodsReceipts FOREIGN KEY (GoodsReceiptId) REFERENCES dbo.GoodsReceipts(Id)
);

CREATE TABLE dbo.StockTransfers (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransfers PRIMARY KEY,
    OrganizationId int NOT NULL,
    FromBranchId int NOT NULL,
    ToBranchId int NOT NULL,
    Status nvarchar(80) NOT NULL,
    Note nvarchar(2000) NULL,
    UserId int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_StockTransfers_Branches_From FOREIGN KEY (FromBranchId) REFERENCES dbo.Branches(Id),
    CONSTRAINT FK_StockTransfers_Branches_To FOREIGN KEY (ToBranchId) REFERENCES dbo.Branches(Id)
);

CREATE TABLE dbo.StockTransferLines (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransferLines PRIMARY KEY,
    StockTransferId int NOT NULL,
    IngredientId int NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    CONSTRAINT FK_StockTransferLines_StockTransfers FOREIGN KEY (StockTransferId) REFERENCES dbo.StockTransfers(Id)
);

CREATE TABLE dbo.Wastages (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Wastages PRIMARY KEY,
    BranchId int NOT NULL,
    IngredientId int NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    Cost decimal(18,2) NOT NULL,
    Reason nvarchar(2000) NOT NULL,
    UserId int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_Wastages_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id)
);

CREATE TABLE dbo.StockAdjustments (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockAdjustments PRIMARY KEY,
    BranchId int NOT NULL,
    IngredientId int NOT NULL,
    QtyDelta decimal(18,3) NOT NULL,
    Reason nvarchar(2000) NOT NULL,
    Status nvarchar(80) NOT NULL,
    UserId int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_StockAdjustments_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id)
);

CREATE TABLE dbo.PhysicalCounts (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhysicalCounts PRIMARY KEY,
    BranchId int NOT NULL,
    Status nvarchar(80) NOT NULL,
    UserId int NOT NULL,
    CountedAt datetime2 NOT NULL,
    Note nvarchar(2000) NULL
);

CREATE TABLE dbo.PhysicalCountLines (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhysicalCountLines PRIMARY KEY,
    PhysicalCountId int NOT NULL,
    IngredientId int NOT NULL,
    SystemQty decimal(18,3) NOT NULL,
    CountedQty decimal(18,3) NOT NULL,
    CONSTRAINT FK_PhysicalCountLines_PhysicalCounts FOREIGN KEY (PhysicalCountId) REFERENCES dbo.PhysicalCounts(Id)
);

CREATE TABLE dbo.Expenses (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Expenses PRIMARY KEY,
    OrganizationId int NOT NULL,
    BranchId int NOT NULL,
    Category nvarchar(80) NOT NULL,
    Amount decimal(18,2) NOT NULL,
    Note nvarchar(2000) NULL,
    UserId int NOT NULL,
    SpentAt datetime2 NOT NULL,
    CONSTRAINT FK_Expenses_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id)
);

CREATE TABLE dbo.ApprovalRequests (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalRequests PRIMARY KEY,
    OrganizationId int NOT NULL,
    BranchId int NULL,
    Type nvarchar(80) NOT NULL,
    EntityKey nvarchar(80) NULL,
    PayloadJson nvarchar(4000) NOT NULL,
    Status nvarchar(80) NOT NULL,
    Reason nvarchar(2000) NULL,
    Amount decimal(18,2) NOT NULL,
    RequestedById int NOT NULL,
    ReviewedById int NULL,
    ReviewNote nvarchar(2000) NULL,
    CreatedAt datetime2 NOT NULL,
    ReviewedAt datetime2 NULL,
    CONSTRAINT FK_ApprovalRequests_Users FOREIGN KEY (RequestedById) REFERENCES dbo.Users(Id)
);

CREATE TABLE dbo.AuditLogs (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    OrganizationId int NOT NULL,
    UserId int NULL,
    Action nvarchar(80) NOT NULL,
    Entity nvarchar(80) NOT NULL,
    EntityId nvarchar(80) NULL,
    Detail nvarchar(2000) NULL,
    Ip nvarchar(80) NULL,
    CreatedAt datetime2 NOT NULL
);

CREATE TABLE dbo.LoginHistories (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoginHistories PRIMARY KEY,
    UserId int NULL,
    Username nvarchar(60) NOT NULL,
    Success bit NOT NULL,
    Ip nvarchar(80) NULL,
    UserAgent nvarchar(400) NULL,
    CreatedAt datetime2 NOT NULL
);

CREATE TABLE dbo.SystemSettings (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
    OrganizationId int NOT NULL,
    [Key] nvarchar(60) NOT NULL,
    Value nvarchar(2000) NOT NULL,
    CONSTRAINT UQ_SystemSettings_Organization_Key UNIQUE (OrganizationId, [Key])
);
GO
