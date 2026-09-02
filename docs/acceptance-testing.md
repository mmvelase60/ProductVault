# Manual acceptance test plan

Run the application with the HTTPS launch profile and use a fresh test account where a test says “user”.

| ID | Scenario | Steps | Expected result |
| --- | --- | --- | --- |
| AT-01 | Register and login | Register with a new email/password; sign out and sign in. | User reaches the authenticated home page. |
| AT-01a | Verify email | Enter the six-digit verification code delivered by Gmail, then sign in. | Verification succeeds; sign-in is allowed only after verification. |
| AT-01b | Recover password | Request a reset link, choose a new password, then sign in. | Reset succeeds and the new password works. |
| AT-02 | Load demo catalogue | In an empty workspace, select **Load demo data**. | Exactly 3 categories and 5 products appear; the dashboard total is R2,387.48. |
| AT-03 | Create valid category | Add `Accessories` with code `ACC001`. | Category appears as Active. |
| AT-04 | Reject invalid category input | Try an invalid code, then submit blank name/code through Swagger. | Clear validation message is displayed; no category is created. |
| AT-05 | Reject duplicate category code | Add another `ACC001` category. | Clear duplicate-code message is displayed. |
| AT-06 | Create product | Add Wireless Mouse, price 249.99, category Accessories. | Product appears with an auto-generated `yyyyMM-001`-style code. |
| AT-07 | Require category | Try saving a product without a category. | Category validation message is displayed. |
| AT-08 | Edit product | Update the name or price. | Product details update successfully. |
| AT-09 | Delete product | Delete a product and accept the confirmation. | Product is removed and success message appears. |
| AT-10 | Image validation | Upload a valid JPG/JFIF, PNG, GIF, or WEBP image; then try unsupported/oversize file. | Valid image saves; invalid input is rejected. |
| AT-11 | Excel import/export | Export products; import a valid worksheet with several valid rows using Name, Description, Category Code, Price. | Export downloads; every imported row has a distinct generated code. |
| AT-12 | Paging | Create/import at least 11 products. | Page 1 contains 10 items and next page is available. |
| AT-13 | Ownership | With user B, attempt user A’s product/category URL. | `404 Not Found`; no data is disclosed. |
| AT-14 | Monitoring | Start Docker monitoring stack and make product requests. | Prometheus target is UP; Grafana dashboard shows metrics. |
