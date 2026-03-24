# DemoWebApi.Auth (Identity + JWT) - Class Library

This project contains the complete vertical slice responsible for:

- Identity management (using OWIN)
- Authentication (JWT)
- Role management (Admin, Users)
- User management
- Permission management
- Admin UI
- JWT token generation and validation 
  - Role-based access control (RBAC) implementation
  - JWT Token will include claims for user roles and permissions to enable fine-grained access control in the Web API.


The Project structure is as shown below:

```
DemoWebApiDB.Auth
│
├── Entities
│   ├── ApplicationUser
│   ├── ApplicationRole
│   ├── Permission
│   └── RolePermission
│
├── Services
│   ├── AuthService
│   ├── PermissionService
│   └── TokenService
│
├── Controllers
│   ├── AuthController
│   ├── UsersController
│   └── RolesController
│
└── Razor UI
    ├── Users
    ├── Roles
    └── Permissions
```

--------

## Phase 1: Setup the Identity and Role Management 

### 01. Add the needed Nuget Package references to the `DemoWebApiDB.Auth` project


### 02. Create the Entities in the `DemoWebApiDB.Data` project
- Entities\ApplicationUser
- Entities\ApplicationRole 
- Entities\Permission
- Entities\RolePermissions (Composite Key: RoleId + PermissionId)

### 03. Define Strongly Typed Permission Constants in the `DemoWebApiDB.Infrastructure` project
- `Constants\Permissions`

### 04. Update the `ApplicationDbContext` to define the customized Identity models and relationships

- Update the `ApplicationDbContext` to include `DbSet` for:
  - `ApplicationUser`
  - `ApplicationRole`
  - `RolePermissions`
  - `Permission`.
- Configure the Composite Key for `RolePermissions` using Fluent API in the `OnModelCreating` method.
- Set up the relationships between `ApplicationRole` and `Permission` through `RolePermissions`.

### 05. Add the `IdentitySeeder`

- Create a new class `IdentitySeeder` in the `DemoWebApiDB.Data` project 
  to seed the database with default roles, permissions, and an admin user.

### 06. Register the Identity Services in the `Program.cs` of the Web API project

- Register the Identity services 
- Ensure that Identity seeding is done before the application starts accepting requests.

### 07. Run the database migration now.

- Add the migration for the Identity models 
- Update the database to create the necessary tables for users, roles, permissions, and their relationships.
- Verify that the tables are created successfully in the database.
- Run the application to ensure that the seeding process occurs without errors 
  and that the default roles, permissions, and admin user are created in the database.
  - Check that `FullName` is added to the `AspNetUsers` table.
  - Check that the `AspNetRoles` table has now the application roles.
  - Check that the `Permissions` table and `RolePermissions` table are populated


--------

## Phase 2: Setup JWT Authentication for Role-Based Access Control (RBAC)

### 08. Configure JWT Configuration in `appsettings.json` and `Program.cs` of the Web API project

- Add a new section for JWT configuration in the `appsettings.json` file of the Web API project. Include settings for:
   - `SecretKey`
   - `Issuer`
   - `Audience`
   - `TokenExpirationMinutes`
 - Add the Nuget Package reference for `Microsoft.AspNetCore.Authentication.JwtBearer` to the Web API project.
 - Register the JWT authentication scheme in the `Program.cs` of the Web API project, using the settings from `appsettings.json`.
 - Add the necessary middleware to enable authentication and authorization in the request pipeline in the `Program.cs` file.

 
 ### 09. Define the LoginDto in the `DemoWebApiDB.Auth` project

- Create a new DTO class `LoginDto` in the `DemoWebApiDB.Auth` project to represent the login request payload


### 10. Define the `TokenService` in the `DemoWebApiDB.Auth` project

- Create a new service class `TokenService` in the `DemoWebApiDB.Auth` project responsible for generating JWT tokens.


### 11. Define the `AuthService` in the `DemoWebApiDB.Auth` project

- Create a new service class `AuthService` in the `DemoWebApiDB.Auth` project responsible for handling authentication logic, 
  including validating user credentials and generating JWT tokens using the `TokenService`.
- Ensure that it implements the Result Pattern to provide consistent responses for success and failure scenarios during authentication.

### 12. Implement the `AuthController` in the `DemoWebApiDB` API project

- Create a new controller class `AuthController` in the `DemoWebApiDB` API project to handle authentication requests.
- Ensure it inherits from BaseApiController and is decorated with the appropriate route attributes.
- Implement the `Login` action method to validate user credentials and return a JWT token upon successful authentication.


### 13. Register the `TokenService` and `AuthService` in the Dependency Injection container

- Register the `TokenService` and `AuthService` in the DI container in the `Program.cs` of the Web API project.


### 14. Check if JWT Authentication is working

- Run the application and test the login endpoint.
- Check if the JWT token is generated successfully and contains the correct claims for user roles and permissions.
- You can use the online tool [jwt.io](https://jwt.io/) to decode the token and verify its contents.

---


## Phase 3: Setup Permission-Based Authorization Policies


### 15. Define a Constant `CustomClaimType` in the `DemoWebApiDB.Infrastructure` project 

Define a Constant `CustomClaimType` in the `DemoWebApiDB.Infrastructure` project 
to represent the claim type used for permissions in the JWT token.  
This helps ensure that there are no "magic strings" to denote the claim type for permissions.


### 16. Define the `PermissionService` in the `DemoWebApiDB.Auth` project

Add a new class `Policies\PermissionRequirement` in the `DemoWebApiDB.Auth`, that implements `IAuthorizationRequirement`.

This represents a permission requirement for authorization policies, used by the authorization middleware.


### 17. Define the `PermissionAuthorizationHandler` in the `DemoWebApiDB.Auth` project

Add a new class `Handlers\PermissionAuthorizationHandler` that inherits from `AuthorizationHandler<PermissionRequirement>`,
in the `DemoWebApiDB.Auth` project.

This handles the evaluation of permission requirements during the authorization process.
  ```
  NOTE:
    - Requirement   → describes what must be satisfied
    - Handler       → checks whether the requirement is satisfied 
  ```

### 18. Define the `PermissionPolicyProvider` in the `DemoWebApiDB.Auth` project

Add a new class `Policies\PermissionPolicyProvider` that inherits from `DefaultAuthorizationPolicyProvider`, 
in the `DemoWebApiDB.Auth` project.

This dynamically creates authorization policies based on permissions defined in the database.

### 19. Register the Authorization services and handlers in the `Program.cs` of the Web API project

In the `Program.cs` of the API Project:

  - Ensure that Authorization services is registered in the DI container:
    ```
        builder.Services.AddAuthorization();
    ```

  - Register the services in the DI Container:
    ```
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    ```

---

## To summarize:

The `PermissionPolicyProvider` intercepts policy requests from ASP.NET Core 
and dynamically builds the policy when the requested policy name matches one of the defined application permissions.
This avoids the need to manually register every single policy in Program.cs.
All one now needs to do is define using the policy like:
    ```
    [Authorize(Roles = "Admin")]
    ```
or
    ```
    [Authorize(Policy = Permissions.CanAddCategory)]
    ```
without having to explicitly register all the policies in Program.cs


Thus, the final workflow is:

```
Angular Client
      │
HTTP Request with JWT
      │
Authentication Middleware ( app.UseAuthentication )
      │
JWT validated → ClaimsPrincipal created
      │
Authorization Middleware ( app.UseAuthorization )
      │
Authorize Attribute detected
      │
PermissionPolicyProvider
      │
PermissionRequirement created
      │
PermissionAuthorizationHandler
      │
Requirement satisfied?
      │
      ├─ YES → Controller Action method executes
      │
      └─ NO  → 403 Forbidden
```

To summarize the expected results for different scenarios when accessing a protected endpoint:

| Scenario	                    | Result           |
|-------------------------------|------------------|
| No token	                    | 401 Unauthorized |
| Token invalid	                | 401 Unauthorized |
| Token valid but no permission	| 403 Forbidden    |


---


# Define the Admin UI for managing Users, Roles, and Permissions in the `DemoWebApiDB` API project!

## Add Razor Pages support in the `Program.cs` of the Web API project

Add the following lines in the `Program.cs` of the Web API project:


### 20. Register Razor Pages Support in the DI Container

```
// Registers Razor Pages support used for administration UI.
builder.Services.AddRazorPages();
````


### 21. Add Razor Pages Middleware to the Request Pipeline
```
app.MapControllers();             // Maps API controller endpoints

app.MapRazorPages();              // Maps Razor Pages endpoints for the admin UI
```

### 22. Setup the Folders for the Admin UI in the `DemoWebApiDB` API project

```
DemoWebApiDB
│
├── Pages
│   ├── _ViewImports.cshtml                 (Razor View)
│   ├── _ViewStart.cshtml                   (Razor View)
│   │
│   └── Admin
│       ├── _AdminLayout.cshtml             (Razor View - Layout for all Admin Pages)
│       │
│       ├── Roles                       ROLE MANAGEMENT PAGES
│       │   ├── Index.cshtml                (Razor Page)  - to list all the Roles
│       │   └── Permissions.cshtml          (Razor Page   - to manage the permissions for a selected role
│       │
│       └── Users                       USER MANAGEMENT PAGES
│           ├── Index.cshtml                (Razor Page)  - to list all Users in the system
│           └── ManageUser.cshtml           (Razor Page)  - to Add/Edit a User and manage the Roles for the User.
```

### 23. Define the Configuration files for the Razor Pages

1. Add `_ViewImports.cshtml` file <br />
   to define the global USINGS for the Razor View Pages in the current and sub-folders.
2. Add the `_AdminLayout.cshtml` file <br />
   to define the UI layout for all the Admin Razor Pages.
3. Add `_ViewStart.cshtml` to register the UI Layout for all the pages in the current and sub-folders.


### 24. Define the Permission Management Screens for the Roles

1. Add the default landing page (`Index.cshtml`) for URL:`\Admin\Roles` <br />
   to list all the application roles.
2. Add the manage permissions page (`Permissions.cshtml`)
   - It allows to configure the permissions of the selected role
   - The user will navigate to this page after selecting an Application Role from the `Index` page.


### 25. Define the User Management Screens for the Users

1. Add the default landing page (`Index.cshtml`) for URL:`\Admin\Users` <br />
   to list all the users in the application.
2. Add the manage users page (`ManageUser.cshtml`)
   to add/edit the user and assign application role(s) to the user.


### 26. Setup the Login and Logout Screens for the Admin Module

1. Add `Login.cshtml` and `Logout.cshtml` 
2. Configure Program.cs to provide support for `CookieAuthentication` & `JwtAuthentication` in the API Project
    - ensure that CookieAuthentication is set as the default, for automatic routing to Login Screen


### 27. Finally, configure the Authication Policy on all API Routes

1. Configure the `BaseApiController` with JWT Authentication Scheme, 
   to ensure Bearer Token is always required to access the all API endpoints. 
2. Set `Login` API endpoint in `AuthController` to allow "anonymous" access.

---
