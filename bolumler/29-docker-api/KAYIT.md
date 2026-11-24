# TZ-4731 · API Docker'da açılmıyor, bilgisayarda çalışıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4731 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Docker |
| Bildiren | Yusuf Çelik, DevOps mühendisi |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), CI, Chrome |
| Durum | Açık |

## Ne oldu

CI hattında ve kendi makinemde `docker compose` ile her şeyi container'da çalıştırdığımda API container'ı açılıp hemen kapanıyor. Müşteri, satıcı ve yönetim siteleri açılıyor ama hiçbirinde veri gelmiyor; her yerde hata kutusu var.

Aynı kodu VS Code'da F5 ile bilgisayarımda çalıştırınca API sorunsuz açılıyor ve siteler veri gösteriyor. Yani kodun kendisi çalışıyor gibi; yalnızca container'da açılmıyor. Ödeme ve bildirim servisleri container'da ayakta; yalnızca API düşüyor. Geçen hafta aynı komutla her şey ayağa kalkıyordu.

## Tekrar etme adımları

1. Yerelde çalışan servisleri ve `npm run dev` pencerelerini kapatın (F5 ile açtığınız API'yi de durdurun).
2. Depo kökünde: `docker compose --profile app up -d --build`
3. `docker compose ps -a` çıktısında `api` servisinin durumuna bakın. Codespaces'te de F5 yerine aynı komutu kullanın.
4. Birkaç dakika sonra http://localhost:5173 adresini açın.

## Beklenen

`api` servisi `running` / `healthy` olur; siteler veri gösterir.

## Gerçekleşen

`api` servisi `exited` durumunda; siteler veri alamıyor ve hata gösteriyor.

## Ek bilgi

- Aynı kod bilgisayarda F5 (ya da `dotnet run`) ile sorunsuz açılıyor.
- Veritabanı ve diğer altyapı servisleri ayakta; sorun yalnızca API container'ında.
- Geçen hafta aynı komutla `api` ayağa kalkıyordu.
- Container'ı yeniden başlatmak ya da komutu tekrar çalıştırmak sonucu değiştirmiyor.

## Bitirdiğinizde

Sistem container'da çalışırken (aynı `--profile app` ile) şunu çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 29
```

---

## English

# TZ-4731 · The API does not start in Docker but runs locally

| Field | Value |
| --- | --- |
| Ticket | TZ-4731 |
| Level | ●● (2/5) |
| Where it shows | Docker |
| Reporter | Yusuf Çelik, DevOps engineer |
| Priority | High |
| Environment | Development environment (Docker Compose), CI, Chrome |
| Status | Open |

### What happened

When I run everything in containers with `docker compose` in the CI pipeline and on my own machine, the API container starts and immediately stops. The storefront, seller and admin sites open, but none of them gets any data; there is an error box everywhere.

When I run the same code on my computer with F5 in VS Code, the API starts without any problem and the sites show data. So the code itself seems to work; it just does not start in a container. The payment and notification services are up in containers; only the API goes down. Last week everything came up with the same command.

### Steps to reproduce

1. Stop locally running services and any `npm run dev` windows (also stop an API you started with F5).
2. From the repository root: `docker compose --profile app up -d --build`
3. Look at the status of the `api` service in the `docker compose ps -a` output. In Codespaces, use the same command instead of F5.
4. After a few minutes open http://localhost:5173.

### Expected

The `api` service is `running` / `healthy`; the sites show data.

### Actual

The `api` service is `exited`; the sites cannot get data and show errors.

### Additional information

- The same code starts without any problem on the computer with F5 (or `dotnet run`).
- The database and the other infrastructure services are up; the problem is only in the API container.
- Last week `api` came up with the same command.
- Restarting the container or running the command again does not change the result.

### When you are done

With the system running in containers (same `--profile app`), run:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 29
```
