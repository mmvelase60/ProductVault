# Manual acceptance test plan

Run the application with the HTTPS launch profile and use a fresh test account where a test says “user”.

| ID | Scenario | Steps | Expected result |
| --- | --- | --- | --- |
| AT-01 | Register and login | Register with a new email/password; sign out and sign in. | User reaches the authenticated home page. |
| AT-02 | Create valid category | Add `Accessories` with code `ACC001`. | Category appears as Active. |
| AT-03 | Reject invalid category code | Add category with code `ACC01`. | Clear `AAA999` format message is displayed. |
| AT-04 | Reject duplicate category code | Add another `ACC001` category. | Clear duplicate-code message is displayed. |
| AT-05 | Create product | Add Wireless Mouse, price 249.99, category Accessories. | Product appears with an auto-generated `yyyyMM-001`-style code. |
| AT-06 | Require category | Try saving a product without a category. | Category validation message is displayed. |
| AT-07 | Edit product | Update the name or price. | Product details update successfully. |
| AT-08 | Delete product | Delete a product and accept the confirmation. | Product is removed and success message appears. |
| AT-09 | Image validation | Upload a valid image; then try unsupported/oversize file. | Valid image saves; invalid input is rejected. |
| AT-10 | Excel import/export | Export products; import a valid worksheet using Name, Description, Category Code, Price. | Export downloads; valid rows import with generated codes. |
| AT-11 | Paging | Create/import at least 11 products. | Page 1 contains 10 items and next page is available. |
| AT-12 | Ownership | With user B, attempt user A’s product/category URL. | `404 Not Found`; no data is disclosed. |
| AT-13 | Monitoring | Start Docker monitoring stack and make product requests. | Prometheus target is UP; Grafana dashboard shows metrics. |
