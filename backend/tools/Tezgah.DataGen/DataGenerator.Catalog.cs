using Tezgah.Application.Common.Text;
using Tezgah.DataGen.Data;
using Tezgah.Domain.Catalog;

namespace Tezgah.DataGen;

public partial class DataGenerator
{
    private async Task WriteCategoriesAndBrandsAsync()
    {
        await using var categories = await BulkWriter.OpenAsync(dataSource, "categories", "id", "parent_id", "name", "slug", "sort_order", "is_active");
        await using var attributes = await BulkWriter.OpenAsync(dataSource, "product_attributes", "id", "category_id", "name", "unit", "is_filterable");
        long nextCategoryId = 1, nextAttributeId = 1;

        async Task WriteGroup(CategoryGroup group, long? parentId, int sort)
        {
            var id = nextCategoryId++;
            await categories.RowAsync(id, parentId, group.Name, SlugHelper.Slugify(group.Name), sort, true);
            var childSort = 0;
            foreach (var child in group.Children) await WriteGroup(child, id, childSort++);
            foreach (var leaf in group.Leaves)
            {
                var leafId = nextCategoryId++;
                await categories.RowAsync(leafId, id, leaf.Name, SlugHelper.Slugify(leaf.Name), childSort++, true);
                var attrIds = new List<long>();
                foreach (var (name, unit, _) in leaf.Attributes)
                {
                    var attrId = nextAttributeId++;
                    await attributes.RowAsync(attrId, leafId, name, unit, true);
                    attrIds.Add(attrId);
                }
                _leaves.Add(new LeafInfo(leafId, leaf, attrIds.ToArray()));
            }
        }

        var rootSort = 0;
        foreach (var root in CategorySeed.Roots) await WriteGroup(root, null, rootSort++);

        // Kurgusal markalar: gerçek markalarla karışmasın diye hece birleşimi
        await using var brands = await BulkWriter.OpenAsync(dataSource, "brands", "id", "name", "slug", "is_active");
        var names = new HashSet<string>();
        var brandCount = Math.Clamp(_p.Products / 50, 40, 400);
        while (names.Count < brandCount)
            names.Add(Pick(TurkishData.BrandSyllablesA) + Pick(TurkishData.BrandSyllablesB));
        long brandId = 1;
        foreach (var name in names.OrderBy(n => n))
            await brands.RowAsync(brandId++, name, SlugHelper.Slugify(name), true);
        _brandCount = brandId - 1;
    }

    private async Task WriteProductsAsync()
    {
        await using var products = await BulkWriter.OpenAsync(dataSource, "products",
            "id", "seller_id", "category_id", "brand_id", "name", "slug", "description", "status", "rating", "review_count", "sold_count", "created_at");
        await using var variants = await BulkWriter.OpenAsync(dataSource, "product_variants",
            "id", "product_id", "sku", "barcode", "color", "size", "price", "list_price", "vat_rate", "weight_kg", "is_active", "created_at");
        await using var images = await BulkWriter.OpenAsync(dataSource, "product_images", "id", "product_id", "url", "alt_text", "sort_order");
        await using var attrValues = await BulkWriter.OpenAsync(dataSource, "product_attribute_values", "id", "product_id", "attribute_id", "value");

        long nextVariantId = 1, nextImageId = 1, nextAttrValueId = 1;
        var slugCounts = new Dictionary<string, int>();

        for (long productId = 1; productId <= _p.Products; productId++)
        {
            var leaf = Pick(_leaves);
            var seller = productId <= 40 ? _sellers[0] : Pick(_sellers);
            var item = Pick(leaf.Seed.Items);
            var qualifier = Pick(leaf.Seed.Qualifiers);
            var brandId = Chance(0.92) ? (long?)_rng.NextInt64(1, _brandCount + 1) : null;
            var name = $"{qualifier} {item}";
            var model = $"{(char)('A' + _rng.Next(26))}{_rng.Next(100, 999)}";
            var fullName = $"{name} {model}";

            var baseSlug = SlugHelper.Slugify(fullName);
            var count = slugCounts.TryGetValue(baseSlug, out var c) ? c + 1 : 1;
            slugCounts[baseSlug] = count;
            var slug = count == 1 ? $"{baseSlug}-p{productId}" : $"{baseSlug}-{count}-p{productId}";

            var status = _rng.NextDouble() switch
            {
                < 0.90 => ProductStatus.Active,
                < 0.94 => ProductStatus.Archived,
                < 0.97 => ProductStatus.PendingApproval,
                < 0.99 => ProductStatus.Draft,
                _ => ProductStatus.Rejected,
            };
            var created = RandomPast(900, 1);
            var description = $"<p>{fullName}, {leaf.Seed.Name.ToLowerInvariant()} kategorisinde öne çıkan ürünlerimizden.</p>" +
                $"<ul><li>{qualifier}</li><li>Kolay iade</li><li>Hızlı kargo</li></ul>";

            await products.RowAsync(productId, seller.Id, leaf.Id, brandId, fullName, slug, description, (int)status, 0m, 0, 0, created);

            // Görseller (yer tutucu)
            var imageCount = _rng.Next(1, 5);
            for (var i = 0; i < imageCount; i++)
                await images.RowAsync(nextImageId++, productId, $"https://picsum.photos/seed/tz{productId}-{i}/600/600", fullName, i);

            // Özellikler
            for (var a = 0; a < leaf.AttributeIds.Length; a++)
                await attrValues.RowAsync(nextAttrValueId++, productId, leaf.AttributeIds[a], Pick(leaf.Seed.Attributes[a].Values));

            // Varyantlar
            var basePrice = leaf.Seed.MinPrice + (decimal)Math.Pow(_rng.NextDouble(), 2) * (leaf.Seed.MaxPrice - leaf.Seed.MinPrice);
            foreach (var (color, size, multiplier) in BuildVariants(leaf.Seed.Variants))
            {
                var price = RoundPrice(basePrice * multiplier);
                var listPrice = Chance(0.35) ? (decimal?)RoundPrice(price * (decimal)(1.1 + _rng.NextDouble() * 0.5)) : null;
                var id = nextVariantId++;
                var sku = $"TZ{seller.Id:D4}-{productId:D7}-{id % 1000:D3}";
                var active = status == ProductStatus.Active && !Chance(0.03);
                await variants.RowAsync(id, productId, sku, Chance(0.8) ? $"869{_rng.NextInt64(1_000_000_000, 9_999_999_999)}" : null,
                    color, size, price, listPrice, leaf.Seed.VatRate, Chance(0.7) ? Math.Round((decimal)(0.1 + _rng.NextDouble() * 5), 3) : null,
                    active, created);
                _variants.Add(new VariantInfo(id, productId, seller.Id, fullName, sku, VariantLabel(color, size), price, leaf.Seed.VatRate, active));
            }
        }

        // Siparişlerde gerçekçi dağılım için popüler ürünler (satışların büyük kısmı az sayıda üründe)
        var activeIdx = Enumerable.Range(0, _variants.Count).Where(i => _variants[i].Active).Select(i => (long)i).ToArray();
        _popularVariantIndexes = activeIdx.OrderBy(_ => _rng.Next()).Take(Math.Max(50, activeIdx.Length / 10)).ToArray();
    }

    private IEnumerable<(string? Color, string? Size, decimal Multiplier)> BuildVariants(VariantKind kind)
    {
        switch (kind)
        {
            case VariantKind.ClothingSize:
            {
                var colors = TurkishData.Colors.OrderBy(_ => _rng.Next()).Take(_rng.Next(1, 4)).ToList();
                var sizes = TurkishData.ClothingSizes.Skip(_rng.Next(0, 2)).Take(_rng.Next(3, 6)).ToList();
                foreach (var c in colors) foreach (var s in sizes) yield return (c, s, 1m);
                break;
            }
            case VariantKind.ShoeSize:
            {
                var color = Pick(TurkishData.Colors);
                var start = _rng.Next(0, 4);
                foreach (var s in TurkishData.ShoeSizes.Skip(start).Take(_rng.Next(4, 7))) yield return (color, s, 1m);
                break;
            }
            case VariantKind.Color:
                foreach (var c in TurkishData.Colors.OrderBy(_ => _rng.Next()).Take(_rng.Next(1, 4))) yield return (c, null, 1m);
                break;
            case VariantKind.Storage:
            {
                var options = TurkishData.Storages.Take(_rng.Next(1, 4)).ToList();
                for (var i = 0; i < options.Count; i++) yield return (Pick(TurkishData.Colors), options[i], 1m + i * 0.18m);
                break;
            }
            case VariantKind.Volume:
            {
                var start = _rng.Next(0, 3);
                var options = TurkishData.Volumes.Skip(start).Take(_rng.Next(1, 3)).ToList();
                for (var i = 0; i < options.Count; i++) yield return (null, options[i], 1m + i * 0.6m);
                break;
            }
            default:
                yield return (null, null, 1m);
                break;
        }
    }
}
