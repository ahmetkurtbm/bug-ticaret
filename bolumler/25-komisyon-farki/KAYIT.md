# TZ-4671 · Satıcı panelindeki komisyon birkaç kuruş farklı

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4671 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Satıcı paneli · Özet |
| Bildiren | Songül Aksoy, satıcı destek uzmanı (bir mağaza sahibinin şikâyetini iletiyor) |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Bir mağaza sahibi yazdı: "Hakediş ekstremdeki komisyon tutarı ile satıcı panelindeki Özet sayfasında 'Son 30 gün' altında gördüğüm komisyon tutmuyor. Fark büyük değil, birkaç kuruş, ama muhasebecim her kuruşu soruyor ve hangisinin doğru olduğunu bilmiyorum."

Mağaza sahibinin hesabıyla (`satici@tezgah.test`) kendim de baktım. Satış tutarı ekstreyle tutuyor; yalnızca komisyon birkaç kuruş farklı, net kazanç da buna bağlı olarak kayıyor. Fark her bakışta aynı; sayfayı yenilemek bir şey değiştirmiyor.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5174 adresinde `satici@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Özet sayfasında "Son 30 gün" altındaki "Satış", "Komisyon" ve "Net kazanç" değerlerini not alın.
4. Hakediş ekstresindeki komisyon tutarıyla karşılaştırın. `kontrol 25` aracı ekstre tutarını hesaplar, paneldeki rakamla karşılaştırır ve iki rakamı yan yana yazar.

## Beklenen

Panelde gösterilen 30 günlük komisyon, hakediş ekstresindeki komisyon tutarıyla kuruşu kuruşuna aynıdır.

## Gerçekleşen

Panelin komisyonu ekstreden birkaç kuruş farklı.

## Ek bilgi

- "Satış" tutarı ekstreyle tutuyor; fark yalnızca komisyonda.
- Fark her bakışta aynı; sayfayı yenilemek değiştirmiyor.
- Geçen ay aynı satıcı için panel ve ekstre tutuyordu; bu ay tutmamaya başladı.
- Satıcının komisyon oranı sabit, değişmedi.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 25
```

---

## English

# TZ-4671 · Seller panel commission is off by a few cents

| Field | Value |
| --- | --- |
| Ticket | TZ-4671 |
| Level | ●● (2/5) |
| Where it shows | Seller panel · Summary |
| Reporter | Songül Aksoy, seller support specialist (relaying a store owner's complaint) |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

A store owner wrote: "The commission amount in my payout statement does not match the commission I see under 'Son 30 gün' ('Last 30 days') on the seller panel's Summary page. The difference is not big, a few kuruş (cents), but my accountant questions every kuruş and I do not know which one is right."

I looked myself with the store owner's account (`satici@tezgah.test`). The sales amount matches the statement; only the commission is a few kuruş off, and the net earnings drift accordingly. The difference is the same every time; refreshing the page changes nothing.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Log in at http://localhost:5174 with `satici@tezgah.test` / `Tezgah123!`.
3. On the Summary page note the "Satış" ("Sales"), "Komisyon" ("Commission") and "Net kazanç" ("Net earnings") values under "Son 30 gün".
4. Compare with the commission amount in the payout statement. The `kontrol 25` tool calculates the statement amount, compares it with the figure on the panel and prints the two figures side by side.

### Expected

The 30-day commission shown on the panel is identical to the commission amount in the payout statement, to the kuruş.

### Actual

The panel's commission is a few kuruş off from the statement.

### Additional information

- The "Satış" amount matches the statement; the difference is only in the commission.
- The difference is the same every time; refreshing does not change it.
- Last month the panel and the statement matched for the same seller; they started to differ this month.
- The seller's commission rate is fixed and has not changed.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 25
```
