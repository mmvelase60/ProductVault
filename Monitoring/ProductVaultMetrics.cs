using Prometheus;

namespace ProductVault.Monitoring;

public static class ProductVaultMetrics
{
    public static readonly Counter CategoriesCreated = Metrics.CreateCounter(
        "productvault_categories_created_total",
        "Total number of categories created.");

    public static readonly Counter ProductsCreated = Metrics.CreateCounter(
        "productvault_products_created_total",
        "Total number of products created, including Excel imports.");

    public static readonly Counter ProductsDeleted = Metrics.CreateCounter(
        "productvault_products_deleted_total",
        "Total number of products deleted.");
}
