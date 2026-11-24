# TZ-4766 · Yönetim göstergesi ara sıra hata veriyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4766 |
| Seviye | ●●● (3/5) |
| Nerede görülüyor | Yönetim paneli · Gösterge |
| Bildiren | Hande Uslu, operasyon müdürü |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Her sabah operasyon toplantısında ekran paylaşarak yönetim panelinin Gösterge sayfasını açıyoruz. Toplantıdaki birkaç kişi sayfayı aynı anda açınca bazılarında rapor yerine hata kutusu çıkıyor; sayfayı yenileyince geçiyor. Hata her seferinde aynı kişilerde değil, rastgele kişilerde çıkıyor.

Ben tek başıma açınca bir kez bile hata görmedim; bu yüzden uzun süre "tarayıcı sorunu" sandık. Ama bu hafta toplantıda dört kişiden üçünde hata çıktı ve yönetime sunum yaparken rakamlar ekrana gelmedi.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Gösterge sayfasını tek başınıza açıp rakamların geldiğini görün.
4. Aynı sayfayı iki ya da üç tarayıcı sekmesinde art arda ve olabildiğince aynı anda yenileyin; birkaç kez tekrarlayın. Hata kutusu zaman zaman çıkar.
5. Bunu elle yapmak zor olduğundan `kontrol 31` aracı göstergeyi aynı anda 36 kez açar ve kaç tanesinin hata verdiğini yazar.

## Beklenen

Gösterge kaç kişi aynı anda açarsa açsın her seferinde raporu gösterir.

## Gerçekleşen

Tek kişi açınca sorun yok; birkaç kişi aynı anda açınca bir kısmı hata kutusu görüyor. Aynı sayfa yeniden denenince çalışıyor.

## Ek bilgi

- Hata her seferinde değil, yalnızca birkaç kişi aynı anda açınca çıkıyor.
- Sayfayı yenilemek hatayı geçiriyor.
- Gösterge dışındaki yönetim sayfalarında (siparişler, kampanyalar) hata görmedik.
- Geçen ay toplantıda hiç hata çıkmıyordu; sorun son zamanlarda başladı.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 31
```

---

## English

# TZ-4766 · The admin dashboard fails now and then

| Field | Value |
| --- | --- |
| Ticket | TZ-4766 |
| Level | ●●● (3/5) |
| Where it shows | Admin panel · Dashboard |
| Reporter | Hande Uslu, operations manager |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

Every morning in the operations meeting we share the screen and open the admin panel's Dashboard page. When several people in the meeting open the page at the same time, some of them get an error box instead of the report; it goes away when the page is refreshed. The error does not hit the same people every time; it hits random people.

When I open it alone I never saw the error even once; so for a long time we thought it was a "browser problem". But this week three of four people in the meeting got the error and the figures did not appear on screen while I was presenting to management.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
3. Open the Dashboard page alone and see that the figures arrive.
4. Refresh the same page in two or three browser tabs one after another, as close to the same moment as you can; repeat several times. The error box shows up from time to time.
5. Since this is hard to do by hand, the `kontrol 31` tool opens the dashboard 36 times at the same moment and prints how many of them failed.

### Expected

However many people open the dashboard at the same time, it shows the report every time.

### Actual

When one person opens it there is no problem; when several people open it at the same time, some of them see an error box. The same page works when tried again.

### Additional information

- The error does not appear every time, only when a few people open it at the same time.
- Refreshing the page makes the error go away.
- We did not see errors on the other admin pages (orders, campaigns).
- Last month no error appeared in the meeting; the problem started recently.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 31
```
