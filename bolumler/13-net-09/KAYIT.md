# TZ-4402 · Eski kullanıcılar giriş yapamıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4402 |
| Seviye | ●● (2/5) |
| Modül | Kimlik doğrulama |
| Bildiren | Kerem Aydın, müşteri destek ekip lideri |
| Öncelik | Kritik |
| Ortam | Test ortamı; yerelde API `--launch-profile http` (müşteri sitesi http://localhost:5173) |
| Durum | Açık |

## Ne oldu

Dünkü sürümden sonra destek hattına giriş şikâyetleri yağıyor. Kullanıcılar doğru şifreyi yazdıklarını söylüyor, ekranda şu çıkıyor:

> E-posta veya şifre hatalı.

Biz de test hesabımızla denedik: `musteri@tezgah.test` giriş yapamıyor. `admin@tezgah.test` ve `satici@tezgah.test` de giremiyor. Şifre `Tezgah123!`, değişmedi.

Garip olan şu: bugün yeni hesap açan birkaç kullanıcı sorunsuz giriş yapıyor. Çıkış yapıp tekrar giriyorlar, sorun yok. Yani şifre sistemi çalışıyor gibi. Hesaplar silinmemiş, veritabanında duruyorlar.

Giriş yapabilenlerle yapamayanları karşılaştırmaya çalıştım, ortak bir şey bulamadım. Şifre sıfırlamayı denedik, o da yardımcı olmadı.

## Tekrar etme adımları

Önkoşul: Örnek veri `main` dalındayken üretilmiş olmalı (`dotnet run --project backend/tools/Tezgah.DataGen -- --profile small --reset`). Veri üretici, bulunduğunuz dalın kodunu kullanır.

1. Altyapıyı başlatın: `docker compose up -d`
2. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. Başka bir terminalde: `cd frontend && npm run dev:storefront`
4. http://localhost:5173/giris adresinde `musteri@tezgah.test` / `Tezgah123!` ile giriş yapmayı deneyin.
5. Aynı sayfadan Üye olun (http://localhost:5173/kayit), yeni bir hesap açın (örneğin `ilkim.deneme@tezgah.test`).
6. Çıkış yapın ve yeni hesapla tekrar giriş yapın.

## Beklenen

Mevcut kullanıcıların hepsi giriş yapabilir. E-postanın büyük ya da küçük harfle yazılması fark etmez (`MUSTERI@tezgah.test` ile de girilir).

## Gerçekleşen

Mevcut hesaplar "E-posta veya şifre hatalı." alıyor. Yeni açılan hesaplar giriş yapabiliyor.

## Ek bilgi

- Geliştirici (Selin) "bende çalışıyor" diyor. Kendi makinesi İngilizce Windows; uygulamayı kendi makinesinde ve bir container'da denemiş, ikisinde de sorun görmemiş.
- Sorun dünkü sürüm yayına alınınca başladı; veritabanında kullanıcı kaydı ya da şifre özeti (hash) değişmedi.
- Giriş yapamayan hesaplardan bazıları: `musteri@`, `admin@`, `satici@` (tezgah.test). Yeni açılan ve girebilen hesaplar: `ilkim.deneme@`, `kemal@`.
- Bugün yeni hesap açan kullanıcı sayısı normalden fazla; "hesabım silinmiş" diye tekrar kayıt olanlar var.

## Bitirdiğinizde

API çalışırken şunu çalıştırın. Kontrol demo hesaplarla, büyük harfli e-postayla ve yeni kayıtla giriş dener:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 13
```

Kontrol geçse bile veri tarafını düşünün: bu dalın kodu çalışırken açılan hesaplar için ne yapmak gerekir? (Yerel veritabanınızda sizin açtığınız hesaplar bunun örneğidir.)

---

## English

# TZ-4402 · Existing users cannot log in

| Field | Value |
| --- | --- |
| Ticket | TZ-4402 |
| Level | ●● (2/5) |
| Module | Authentication |
| Reporter | Kerem Aydın, customer support team lead |
| Priority | Critical |
| Environment | Test environment; locally the API with `--launch-profile http` (storefront at http://localhost:5173) |
| Status | Open |

### What happened

Since yesterday's release, login complaints are pouring into the support line. Users say they type the right password, and the screen says (in Turkish):

> E-posta veya şifre hatalı. ("Email or password is wrong.")

We tried with our test account too: `musteri@tezgah.test` cannot log in. `admin@tezgah.test` and `satici@tezgah.test` cannot either. The password is `Tezgah123!` and has not changed.

The strange part: a few users who created new accounts today log in without trouble. They sign out and sign in again, no problem. So the password system seems to work. The accounts were not deleted; they are in the database.

I tried to compare users who can log in with those who cannot and found nothing in common. We tried password reset and it did not help.

### Steps to reproduce

Precondition: the sample data must have been generated while on `main` (`dotnet run --project backend/tools/Tezgah.DataGen -- --profile small --reset`). The data generator uses the code of the branch you are on.

1. Start the infrastructure: `docker compose up -d`
2. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. In another terminal: `cd frontend && npm run dev:storefront`
4. At http://localhost:5173/giris try to sign in with `musteri@tezgah.test` / `Tezgah123!`.
5. From the same site register a new account (http://localhost:5173/kayit), for example `ilkim.deneme@tezgah.test`.
6. Sign out and sign in again with the new account.

### Expected

All existing users can log in. It does not matter whether the email is typed in upper or lower case (`MUSTERI@tezgah.test` also works).

### Actual

Existing accounts get "E-posta veya şifre hatalı.". Newly created accounts can log in.

### Additional information

- The developer (Selin) says "it works on my machine". Her machine runs English Windows; she tried the application on her machine and in a container, and saw no problem in either.
- The problem started when yesterday's release went live; no user record or password hash changed in the database.
- Some accounts that cannot log in: `musteri@`, `admin@`, `satici@` (tezgah.test). New accounts that can log in: `ilkim.deneme@`, `kemal@`.
- More users than usual registered today; some registered again because they thought "my account was deleted".

### When you are done

Run this while the API is running. The check tries the demo accounts, an upper-case email, and a fresh registration:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 13
```

Even if the check passes, think about the data side: what has to be done for accounts created while this branch's code was running? (Accounts you created in your local database are an example.)
