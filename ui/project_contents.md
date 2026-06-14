# Warehouse Management System (WMS Pro) - Project Contents

This document outlines the features and components of the WMS Pro project, as extracted from the UI screenshots.

## 1. Authentication & User Management
*   **Sign In Page:**
    *   Fields: Email Address, Password.
    *   Features: "Remember me" checkbox, "Forgot password?" link, Link to Register.
*   **Create Account (Registration) Page:**
    *   Fields: Full Name, Email Address, Role selection dropdown (e.g., Warehouse Employee), Password, Confirm Password.
    *   Features: Terms of Service and Privacy Policy agreement checkbox.

## 2. Layout & Navigation
*   **Sidebar Menu:** Dashboard, Products, Warehouses, Inventory, Transactions, Orders, Logout.
*   **Top Header:** Project Logo & Name (WMS Pro), User Profile snippet (Name, Email, Avatar).

## 3. Core Modules & Pages

### Dashboard
*   **KPI Cards:** Total Products, Total Stock, Low Stock Alerts, Total Warehouses.
*   **Charts:**
    *   Stock Movement Trend (Line Chart over months).
    *   Stock IN vs OUT (Bar Chart comparing IN and OUT over months).
*   **Recent Activities Table:** Shows recent actions (Stock IN, Stock OUT, Transfer) with details (Product, Quantity, Warehouse, Time).

### Products Management
*   **Products List View:**
    *   Search bar for searching by name or SKU.
    *   Data Table: Name, SKU, Category, Price, Quantity, Actions (Edit, Delete).
    *   "Add Product" action button.
*   **Add/Edit Product Modals:**
    *   Fields: Product Name, SKU, Category, Price, Quantity.

### Warehouses Management
*   **Warehouses View:**
    *   Grid layout displaying warehouse cards.
    *   Card details: Warehouse Name, Location, Phone, Email, Capacity Usage (visual progress bar with current/max units), Manager Name.
    *   "Add Warehouse" action button.
*   **Add/Edit Warehouse Modals:**
    *   Fields: Warehouse Name, Capacity (units), Location, Manager Name, Phone, Email.

### Inventory
*   **Inventory View:**
    *   Filters: Dropdowns for filtering by specific Product and specific Warehouse.
    *   Data Table: Product Name, SKU, Warehouse, Quantity, Last Updated date.
    *   "Export" action button.

### Transactions
*   **Transactions View:**
    *   Summary Cards: Total Stock IN, Total Stock OUT, Total Transfers.
    *   Filters: Search by product name/SKU, Filter by Transaction Type (All, IN, OUT, Transfer).
    *   Data Table: Type (Badge: IN/OUT/TRANSFER), Product, SKU, Quantity, Warehouse (Source/Destination), Date & Time, Reference Number (PO, SO, TR).
    *   "Export" action button.

### Orders
*   **Orders View:**
    *   Summary Cards: Total Orders, Purchase Orders, Sales Orders (with total amounts).
    *   Tabs: All Orders, Purchase Orders, Sales Orders.
    *   Data Table: Order Number, Type (Purchase/Sales), Supplier/Customer, Items count, Total Amount, Status (Completed, Processing, Pending, Cancelled), Date, Actions (View).
    *   "Create Order" action button.

## 4. UI/UX Elements
*   **Status Indicators:** Color-coded badges for statuses (Green for Completed/Stock IN, Red for Cancelled/Stock OUT, Blue for Transfers/Purchase, Yellow for Pending).
*   **Modals:** Used consistently for Add/Edit forms (Products, Warehouses) to keep users in context.
*   **Data Visualization:** Clear and modern charts for quick trend analysis.
