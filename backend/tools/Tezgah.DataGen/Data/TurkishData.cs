namespace Tezgah.DataGen.Data;

public static class TurkishData
{
    public static readonly Dictionary<string, string[]> Districts = new()
    {
        ["İstanbul"] = ["Kadıköy", "Beşiktaş", "Üsküdar", "Bakırköy", "Ataşehir", "Şişli", "Pendik", "Esenyurt", "Maltepe", "Başakşehir"],
        ["Ankara"] = ["Çankaya", "Keçiören", "Yenimahalle", "Etimesgut", "Mamak", "Sincan"],
        ["İzmir"] = ["Karşıyaka", "Bornova", "Konak", "Buca", "Çiğli", "Bayraklı"],
        ["Bursa"] = ["Nilüfer", "Osmangazi", "Yıldırım", "Mudanya"],
        ["Antalya"] = ["Muratpaşa", "Konyaaltı", "Kepez", "Alanya"],
        ["Konya"] = ["Selçuklu", "Meram", "Karatay"],
        ["Adana"] = ["Seyhan", "Çukurova", "Yüreğir"],
        ["Kocaeli"] = ["İzmit", "Gebze", "Darıca", "Körfez"],
        ["Gaziantep"] = ["Şahinbey", "Şehitkamil"],
        ["Kayseri"] = ["Melikgazi", "Kocasinan", "Talas"],
        ["Eskişehir"] = ["Odunpazarı", "Tepebaşı"],
        ["Trabzon"] = ["Ortahisar", "Akçaabat"],
    };

    /// <summary>Nüfusa kabaca orantılı şehir ağırlıkları.</summary>
    public static readonly (string City, float Weight)[] CityWeights =
    [
        ("İstanbul", 0.30f), ("Ankara", 0.13f), ("İzmir", 0.11f), ("Bursa", 0.07f), ("Antalya", 0.07f),
        ("Konya", 0.05f), ("Adana", 0.05f), ("Kocaeli", 0.05f), ("Gaziantep", 0.05f), ("Kayseri", 0.04f),
        ("Eskişehir", 0.04f), ("Trabzon", 0.04f),
    ];

    public static readonly string[] Streets =
    [
        "Atatürk Cad.", "Cumhuriyet Mah.", "İnönü Sok.", "Gazi Bulvarı", "Lale Sok.", "Menekşe Sok.",
        "Fatih Mah.", "Yıldız Cad.", "Barış Sok.", "Çınar Sok.", "Kardelen Sok.", "Zafer Mah.",
    ];

    public static readonly string[] TaxOffices =
    [
        "Kadıköy", "Beşiktaş", "Çankaya", "Karşıyaka", "Nilüfer", "Muratpaşa", "Selçuk", "Seyhan", "Kozyatağı", "Mecidiyeköy",
    ];

    public static readonly string[] Colors = ["Siyah", "Beyaz", "Lacivert", "Gri", "Bej", "Kırmızı", "Yeşil", "Mavi", "Pembe", "Kahverengi"];
    public static readonly string[] ClothingSizes = ["XS", "S", "M", "L", "XL", "XXL"];
    public static readonly string[] ShoeSizes = ["36", "37", "38", "39", "40", "41", "42", "43", "44", "45"];
    public static readonly string[] Storages = ["128 GB", "256 GB", "512 GB", "1 TB"];
    public static readonly string[] Volumes = ["50 ml", "100 ml", "250 ml", "500 ml", "1 L"];

    public static readonly string[] Carriers = ["Hızlı Kargo", "Yurtiçi Lojistik", "Anadolu Kargo", "Ekspres Paket"];

    public static readonly string[] PositiveComments =
    [
        "Ürün anlatıldığı gibi, çok memnun kaldım.", "Kargo çok hızlıydı, paketleme özenliydi.",
        "Fiyatına göre kalitesi çok iyi.", "İkinci kez alıyorum, tavsiye ederim.", "Beden tablosu doğru, tam oldu.",
        "Satıcı çok ilgili, sorularıma hemen döndü.", "Rengi fotoğraftaki gibi.", "Beklediğimden kaliteli çıktı.",
    ];
    public static readonly string[] NeutralComments =
    [
        "Fena değil ama kargo biraz gecikti.", "Ürün güzel, kutusu ezik geldi.", "Bir beden büyük almanızı öneririm.",
        "Fiyatına göre idare eder.",
    ];
    public static readonly string[] NegativeComments =
    [
        "Fotoğraftakinden farklı geldi.", "İki hafta sonra bozuldu, iade ettim.", "Kumaşı çok ince.",
        "Kargo 10 gün sürdü, satıcı cevap vermedi.",
    ];

    public static readonly string[] BrandSyllablesA = ["Lav", "Nex", "Kor", "Mar", "Sel", "Ten", "Ari", "Vol", "Zen", "Pol", "Bel", "Tur", "Ela", "Rin", "Kal", "Dor"];
    public static readonly string[] BrandSyllablesB = ["ora", "ix", "ta", "en", "via", "ar", "us", "ia", "on", "eks", "ova", "is", "ana", "et"];
}
