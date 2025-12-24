RESTful Web API for managing a product catalog with CRUD functionality and JWT authentication.

1) Database configuration (MySQL Workbench)

   CREATE DATABASE my_app_db;
   CREATE USER 'app_user'@'localhost' IDENTIFIED BY 'SecurePass123!';
   GRANT ALL PRIVILEGES ON my_app_db.* TO 'app_user'@'localhost';
   FLUSH PRIVILEGES;

2) Performance check

    - Swagger UI: http://localhost:5184/swagger

    - Health Check: http://localhost:5184/health

    - Test API: http://localhost:5184/api/test

3) Main Product Endpoints

    - GET /api/products - get product list

    - GET /api/products/{id} - get product by ID

    - POST /api/products - create product

    - PUT /api/products/{id} - update product

    - DELETE /api/products/{id} - delete product
   
4) Config (appsettings.json)

{

    "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=my_app_db;User=app_user;Password=SecurePass123!;Charset=utf8mb4;"
    },
    "JwtSettings": {
    "Key": "YourSuperSecretKeyForJWTTokenGeneration_Minimum32Chars!"
    }
}