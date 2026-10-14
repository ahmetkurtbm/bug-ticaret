namespace Tezgah.DataGen.Data;

public enum VariantKind { None, ClothingSize, ShoeSize, Color, Storage, Volume }

/// <summary>Yaprak kategori: ürün adı kalıbı, fiyat aralığı, KDV ve varyant türü.</summary>
public record LeafCategory(
    string Name,
    string[] Items,
    string[] Qualifiers,
    decimal MinPrice,
    decimal MaxPrice,
    decimal VatRate,
    VariantKind Variants,
    (string Name, string? Unit, string[] Values)[] Attributes);

public record CategoryGroup(string Name, CategoryGroup[] Children, LeafCategory[] Leaves);

public static class CategorySeed
{
    private static readonly (string, string?, string[])[] ClothingAttrs =
    [
        ("Kumaş", null, ["Pamuk", "Keten", "Polyester", "Viskon", "Yün karışımlı"]),
        ("Kalıp", null, ["Slim Fit", "Regular", "Oversize", "Relaxed"]),
    ];
    private static readonly (string, string?, string[])[] ShoeAttrs =
    [
        ("Taban", null, ["Kauçuk", "EVA", "Poliüretan"]),
        ("Üst malzeme", null, ["Deri", "Süet", "Tekstil", "Suni deri"]),
    ];

    public static readonly CategoryGroup[] Roots =
    [
        new("Kadın", [], [
            new("Elbise", ["Elbise", "Midi Elbise", "Maxi Elbise", "Gömlek Elbise"], ["Çiçek Desenli", "Düz", "Kruvaze", "Fırfırlı", "Saten"], 180, 2400, 10, VariantKind.ClothingSize, ClothingAttrs),
            new("Bluz & Gömlek", ["Bluz", "Gömlek", "Crop Bluz"], ["Basic", "Volanlı", "Oversize", "Keten"], 120, 1200, 10, VariantKind.ClothingSize, ClothingAttrs),
            new("Kadın Pantolon", ["Pantolon", "Jean", "Kumaş Pantolon", "Tayt"], ["Yüksek Bel", "Wide Leg", "Mom Fit", "Paça Yırtmaçlı"], 200, 1500, 10, VariantKind.ClothingSize, ClothingAttrs),
            new("Kadın Dış Giyim", ["Kaban", "Trençkot", "Şişme Mont", "Blazer Ceket"], ["Kuşaklı", "Kapüşonlu", "Su İtici", "Oversize"], 600, 6500, 10, VariantKind.ClothingSize, ClothingAttrs),
        ]),
        new("Erkek", [], [
            new("Erkek Gömlek", ["Gömlek", "Oxford Gömlek", "Keten Gömlek"], ["Slim Fit", "Regular Fit", "Uzun Kollu", "Kısa Kollu"], 200, 1400, 10, VariantKind.ClothingSize, ClothingAttrs),
            new("Tişört & Polo", ["Tişört", "Polo Yaka Tişört", "Basic Tişört"], ["Bisiklet Yaka", "V Yaka", "Baskılı", "Oversize"], 90, 700, 10, VariantKind.ClothingSize, ClothingAttrs),
            new("Erkek Pantolon", ["Chino Pantolon", "Jean", "Eşofman Altı", "Kargo Pantolon"], ["Slim Fit", "Straight", "Jogger", "Likralı"], 220, 1600, 10, VariantKind.ClothingSize, ClothingAttrs),
            new("Erkek Dış Giyim", ["Mont", "Kaban", "Deri Ceket", "Yelek"], ["Kapüşonlu", "Şişme", "Su Geçirmez", "Astarlı"], 700, 7500, 10, VariantKind.ClothingSize, ClothingAttrs),
        ]),
        new("Ayakkabı & Çanta", [], [
            new("Spor Ayakkabı", ["Spor Ayakkabı", "Koşu Ayakkabısı", "Sneaker"], ["Hafif", "Nefes Alan", "Günlük", "Yüksek Taban"], 400, 4500, 20, VariantKind.ShoeSize, ShoeAttrs),
            new("Bot & Çizme", ["Bot", "Çizme", "Chelsea Bot"], ["Deri", "Su Geçirmez", "Kürklü", "Bağcıklı"], 700, 5500, 20, VariantKind.ShoeSize, ShoeAttrs),
            new("Çanta", ["Omuz Çantası", "Sırt Çantası", "Laptop Çantası", "Cüzdan"], ["Deri", "Su Geçirmez", "Çok Bölmeli", "Minimal"], 150, 3500, 20, VariantKind.Color, []),
        ]),
        new("Elektronik", [
            new("Telefon & Aksesuar", [], [
                new("Cep Telefonu", ["Akıllı Telefon"], ["6.5 inç", "6.7 inç", "5G", "Çift Hatlı"], 5500, 75000, 20, VariantKind.Storage, [("Ekran", "inç", ["6.1", "6.5", "6.7"]), ("Batarya", "mAh", ["4500", "5000", "6000"])]),
                new("Kulaklık", ["Kablosuz Kulaklık", "Bluetooth Kulaklık", "Kulak Üstü Kulaklık"], ["Gürültü Önleyici", "40 Saat Pil", "Mikrofonlu", "Su Geçirmez"], 250, 9000, 20, VariantKind.Color, [("Pil ömrü", "saat", ["20", "30", "40", "60"])]),
                new("Powerbank & Şarj", ["Powerbank", "Hızlı Şarj Adaptörü", "Kablosuz Şarj Cihazı"], ["20000 mAh", "65W", "Type-C", "Magnetik"], 150, 2500, 20, VariantKind.Color, []),
            ]),
            new("Bilgisayar & Tablet", [], [
                new("Dizüstü Bilgisayar", ["Dizüstü Bilgisayar", "Oyun Bilgisayarı", "Ultrabook"], ["15.6 inç", "14 inç", "16 GB RAM", "SSD"], 15000, 95000, 20, VariantKind.Storage, [("RAM", "GB", ["8", "16", "32"]), ("Ekran", "inç", ["14", "15.6", "16"])]),
                new("Tablet", ["Tablet", "Çizim Tableti"], ["10.1 inç", "Kalemli", "Wi-Fi", "LTE"], 3500, 45000, 20, VariantKind.Storage, []),
            ]),
            new("Giyilebilir Teknoloji", [], [
                new("Akıllı Saat", ["Akıllı Saat", "Akıllı Bileklik"], ["Nabız Ölçer", "GPS'li", "AMOLED Ekran", "Su Geçirmez"], 600, 18000, 20, VariantKind.Color, [("Kasa", "mm", ["40", "42", "44", "46"])]),
            ]),
        ], []),
        new("Ev & Yaşam", [], [
            new("Ev Tekstili", ["Nevresim Takımı", "Havlu Seti", "Battaniye", "Perde"], ["Pamuklu", "Çift Kişilik", "Tek Kişilik", "Ranforce"], 150, 3500, 10, VariantKind.Color, []),
            new("Mutfak", ["Tencere Seti", "Tava", "Çatal Kaşık Seti", "Saklama Kabı Seti"], ["Granit", "Çelik", "Döküm", "12 Parça"], 150, 9000, 20, VariantKind.None, []),
            new("Küçük Ev Aletleri", ["Kahve Makinesi", "Air Fryer", "Blender", "Elektrikli Süpürge", "Ütü"], ["1500W", "Dijital Ekranlı", "Kablosuz", "Paslanmaz Çelik"], 600, 25000, 20, VariantKind.Color, [("Güç", "W", ["800", "1200", "1500", "2000"])]),
            new("Aydınlatma", ["Avize", "Lambader", "Masa Lambası", "LED Şerit"], ["Modern", "Ahşap", "Metal", "Uzaktan Kumandalı"], 150, 6000, 20, VariantKind.None, []),
        ]),
        new("Kozmetik", [], [
            new("Parfüm", ["Eau de Parfum", "Eau de Toilette", "Vücut Spreyi"], ["Kadın", "Erkek", "Unisex", "Odunsu"], 250, 5500, 20, VariantKind.Volume, []),
            new("Cilt Bakımı", ["Nemlendirici", "Serum", "Güneş Kremi", "Yüz Temizleme Jeli"], ["Hassas Ciltler İçin", "SPF 50", "Hyaluronik Asitli", "Yağsız"], 90, 1800, 20, VariantKind.Volume, []),
            new("Saç Bakımı", ["Şampuan", "Saç Kremi", "Saç Maskesi", "Saç Kurutma Makinesi"], ["Keratinli", "Kepeğe Karşı", "Onarıcı", "İyonik"], 70, 4500, 20, VariantKind.Volume, []),
        ]),
        new("Anne & Bebek", [], [
            new("Bebek Bezi & Islak Mendil", ["Bebek Bezi", "Islak Mendil", "Külot Bez"], ["Jumbo Paket", "Aylık Paket", "Hassas"], 150, 1500, 10, VariantKind.None, []),
            new("Bebek Arabası & Oto Koltuğu", ["Bebek Arabası", "Oto Koltuğu", "Puset"], ["Travel Sistem", "Katlanabilir", "Isofix'li"], 2000, 35000, 20, VariantKind.Color, []),
            new("Oyuncak", ["Eğitici Oyuncak", "Peluş Oyuncak", "Yapboz", "Uzaktan Kumandalı Araba"], ["3+ Yaş", "Ahşap", "Sesli", "1000 Parça"], 80, 3500, 20, VariantKind.None, []),
        ]),
        new("Spor & Outdoor", [], [
            new("Fitness", ["Yoga Matı", "Dambıl Seti", "Direnç Bandı", "Koşu Bandı"], ["Kaymaz", "Ayarlanabilir", "Katlanabilir", "Profesyonel"], 100, 30000, 20, VariantKind.None, []),
            new("Kamp", ["Kamp Çadırı", "Uyku Tulumu", "Kamp Sandalyesi", "Termos"], ["4 Kişilik", "Su Geçirmez", "Katlanabilir", "Paslanmaz"], 150, 12000, 20, VariantKind.Color, []),
            new("Bisiklet", ["Dağ Bisikleti", "Şehir Bisikleti", "Elektrikli Scooter"], ["26 Jant", "21 Vites", "Alüminyum Kadro", "Katlanır"], 4500, 45000, 20, VariantKind.Color, []),
        ]),
        new("Kitap & Kırtasiye", [], [
            new("Kitap", ["Roman", "Çocuk Kitabı", "Kişisel Gelişim Kitabı", "Tarih Kitabı"], ["Ciltli", "Karton Kapak", "Özel Baskı", "Cep Boy"], 60, 650, 0, VariantKind.None, []),
            new("Kırtasiye", ["Defter", "Kalem Seti", "Ajanda", "Boya Kalemi"], ["A4", "Kareli", "Çizgili", "36 Renk"], 30, 900, 20, VariantKind.None, []),
        ]),
        new("Süpermarket", [], [
            new("Temel Gıda", ["Zeytinyağı", "Pirinç", "Makarna", "Bal"], ["Sızma", "Organik", "5 kg", "Süzme"], 40, 1400, 1, VariantKind.Volume, []),
            new("İçecek", ["Türk Kahvesi", "Filtre Kahve", "Siyah Çay", "Yeşil Çay"], ["Orta Kavrulmuş", "1 kg", "500 g", "Demlik Poşet"], 60, 1200, 1, VariantKind.None, []),
            new("Temizlik", ["Bulaşık Deterjanı", "Çamaşır Deterjanı", "Yüzey Temizleyici", "Çamaşır Suyu"], ["Sıvı", "Toz", "Konsantre", "Lavanta Kokulu"], 50, 900, 20, VariantKind.Volume, []),
        ]),
    ];
}
