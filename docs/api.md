# API documentation

Base path: `/api`  
Authentication: ASP.NET Core Identity session cookie. Sign in through the MVC application before using these same-origin endpoints.

All endpoints scope data to the currently authenticated user. A user cannot retrieve or modify another user's records by changing an ID.

## Categories

### List categories

`GET /api/categories`

Returns the authenticated user's categories ordered by name.

### Create category

`POST /api/categories`

```json
{
  "name": "Accessories",
  "categoryCode": "ACC001",
  "isActive": true
}
```

Returns `201 Created`. Category codes are case-normalized to uppercase and must match `AAA999`. Duplicate codes for the same user return `409 Conflict`.

### Update category

`PUT /api/categories/{id}`

Uses the same request body as create. Returns `204 No Content`, `404 Not Found` for a non-owned/missing record, or `409 Conflict` for a duplicate code.

## Products

### List products

`GET /api/products?page=1&pageSize=10`

`page` defaults to 1; `pageSize` defaults to 10 and is capped at 100.

```json
{
  "items": [
    {
      "productId": 1,
      "productCode": "202608-001",
      "name": "Wireless Mouse",
      "description": "Ergonomic Bluetooth mouse",
      "price": 249.99,
      "categoryId": 1,
      "categoryName": "Accessories",
      "imagePath": null
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1
}
```

### Create product

`POST /api/products`

```json
{
  "name": "Wireless Mouse",
  "description": "Ergonomic Bluetooth mouse",
  "price": 249.99,
  "categoryId": 1
}
```

Returns `201 Created`. The server assigns `ProductCode` using the `yyyyMM-###` format. The selected category must be active and owned by the user.

### Update product

`PUT /api/products/{id}`

Uses the same request body as create. Returns `204 No Content`, `400 Bad Request` for invalid input/category, or `404 Not Found` when the product is not owned by the user.

### Delete product

`DELETE /api/products/{id}`

Returns `204 No Content`, or `404 Not Found` for a non-owned/missing record.

## Response conventions

| Status | Meaning |
| --- | --- |
| 200 | Successful list request. |
| 201 | Resource created. |
| 204 | Update/delete succeeded. |
| 400 | Input failed validation. |
| 401 | Sign-in is required. |
| 404 | Record does not exist or does not belong to the user. |
| 409 | A uniqueness or concurrency conflict occurred. |
