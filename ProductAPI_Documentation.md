🛒 Product API – Documentation

Version: 1.0
Author: Mahmut Metindoğan
Email: smetindogan@gmail.com

Repository: https://github.com/1MAM2/Asp.Net_Web_Api

Framework: ASP.NET Core 8.0
Database: MySQL
ORM: Entity Framework Core

📘 Overview

Product API is a modular backend system built with ASP.NET Core.
It provides ready-to-use e-commerce features including:

Product & Category Management

User Authentication & Role Management

Order Management

Payment Integration

Dashboard Data

Soft Delete & Stock Management

It can be used with any frontend: React, Vue, Angular, or plain HTML/JS.

⚙️ Features
Feature Description
🧾 Product CRUD Add, update, delete, list products with categories & galleries
👤 User System Register, login, JWT-based authentication, role management
🛍️ Order System Create and manage orders with status updates
💳 Payment Integration Iyzipay sandbox 3D secure support
🔒 JWT Auth Secure login and token validation
🚫 Soft Delete Products & Users are soft deleted instead of permanent removal
⚡ Optimized Queries Eager loading with EF Include for faster responses
🧰 Technologies Used

ASP.NET Core 8

Entity Framework Core 8

MySQL

JWT Token Authentication

C# 12 / .NET 8

LINQ & Async Programming

✅ Note: MySQL database is required; DefaultConnection must point to the user’s MySQL instance.

🪜 Installation Guide
1️⃣ Clone or Download
git clone https://github.com/1MAM2/Asp.Net_Web_Api.git

2️⃣ Configure Database

Open appsettings.json and update:

"ConnectionStrings": {
"DefaultConnection": "server=localhost;database=productdb;user=root;password=yourpassword;"
}

⚠️ User must create productdb database manually or via migration.

3️⃣ Apply Migrations
dotnet ef database update

4️⃣ Run API
dotnet run

Default endpoints:

HTTP: http://localhost:5039



🔑 Authentication Endpoints
Method Endpoint Description

POST /api/Auth/register Register new user

GET /api/Auth/{id} Get user by ID

GET /api/Auth/verify-email Verify email token

POST /api/Auth/login Login user and get JWT token

POST /api/Auth/logout Logout user

POST /api/Auth/refresh-token Refresh JWT token

POST /api/Auth/confirm-email Confirm user email

GET /api/Auth/protectedRoute Protected route example

🗂️ Category Endpoints

Method Endpoint Description

GET /api/Category List all categories

POST /api/Category Create category

GET /api/Category/{catid}/products List products of a category

GET /api/Category/{catid} Get category by ID

DELETE /api/Category/{catid} Delete category

PUT /api/Category/{catid} Update category

📊 Dashboard Endpoints

Method Endpoint Description

GET /api/Dashboard Get dashboard statistics

🛍️ Order Endpoints

Method Endpoint Description

GET /api/Order Get all orders (Admin)

GET /api/Order/user-getall-orders Get all orders of current user

POST /api/Order/create-order Create new order

GET /api/Order/{id} Get order by ID

DELETE /api/Order/{id} Delete order

PATCH /api/Order/{id}/status Update order status

💳 Payment Endpoints

Method Endpoint Description

POST /api/Payment/pay/{transactionId} Make payment request

POST /api/Payment/pay-callback Payment callback from Iyzipay

🛒 Product Endpoints

Method Endpoint Description

GET /api/Product List all products

POST /api/Product Create product

GET /api/Product/{id} Get product by ID

PUT /api/Product/{id} Update product

DELETE /api/Product/{id} Soft delete product

PUT /api/Product/{id}/stock Update product stock

👤 User Endpoints

Method Endpoint Description

GET /api/User List all users

GET /api/User/me Get current user info

DELETE /api/User/deleteAccount Delete own account

PUT /api/User/soft-delete/{id} Soft delete any user

PUT /api/User/updateuser Update current user info

PUT /api/User/change-role Change role of a user

📄 License

Commercial use allowed

Reselling under a different name is not allowed

📧 Support

Email: smetindogan@gmail.com
