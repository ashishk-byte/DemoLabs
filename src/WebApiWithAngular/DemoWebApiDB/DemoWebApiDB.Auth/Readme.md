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

### 15. Define the `PermissionService` in the `DemoWebApiDB.Auth` project

- Add a new class `Policies\PermissionRequirement` that implements `IAuthorizationRequirement` 
  to represent a permission requirement for authorization policies.
- Add a new class `Handlers\PermissionHandler` that inherits from `AuthorizationHandler<PermissionRequirement>` 
  to handle the evaluation of permission requirements during authorization.
  ```
  NOTE:
    - Requirement   → describes what must be satisfied
    - Handler       → checks whether the requirement is satisfied 
  ```
- Add a new class `Policies\PermissionPolicyProvider` that inherits from `DefaultAuthorizationPolicyProvider` 
  to dynamically create authorization policies based on permissions defined in the database.
- In the `Program.cs` of the API Project:
  - Ensure that Authorization services is registered in the DI container:
    ```
        builder.Services.AddAuthorization();
    ```
  - Register the services in the DI Container:
    ```
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    ```

 Thus, the final workflow will be:

```
Angular Request
        │
JWT Authentication Middleware
        │
Authorization Middleware
        │
PermissionPolicyProvider
        │
PermissionAuthorizationHandler
        │
Controller Endpoint
```
