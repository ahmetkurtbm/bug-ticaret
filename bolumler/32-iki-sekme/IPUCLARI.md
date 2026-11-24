# İpuçları · Bölüm 32

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Önce iki sekmede ne olduğunu sırayla izleyin. İki sekmede de F12 → Network sekmesini açın ve *Preserve log* kutusunu işaretleyin. Süre dolduktan sonra birinci sekmede bir sayfaya geçin, ardından ikincisinde; `/api/auth/refresh` isteklerini ve durum kodlarını yan yana yazın. Hangi sekmenin isteği `200`, hangisininki `401`? İkisi aynı veriyle mi gitti?

*EN:* First follow in order what happens in the two tabs. Open F12 → Network in both tabs and tick *Preserve log*. After the time has run out, go to a page in the first tab and then in the second; write the `/api/auth/refresh` requests and their status codes side by side. Which tab's request is `200` and which is `401`? Did both go with the same data?

## İpucu 2

Sunucu oturumu her yenilemede **yeni bir yenileme anahtarı** verip eskisini geçersiz kılıyor (`AuthService.RefreshAsync`, `IssueAsync`); aynı anahtarla ikinci kez gelen istek reddedilir. Birinci sekme yenileyince yeni oturumu tarayıcının ortak deposuna yazar; ikinci sekmenin kendi belleğindeki kopyası ise artık geçersiz anahtarı taşır. F12 → Application sekmesinde depolanan oturum anahtarının (`tezgah.session`) değerinin sekmeler arasında nasıl değiştiğine bakın. İkinci sekme neyi bilmiyor?

*EN:* On each refresh the server issues a **new refresh key** and invalidates the old one (`AuthService.RefreshAsync`, `IssueAsync`); a second request carrying the same key is rejected. When the first tab refreshes, it writes the new session to the browser's shared store; the second tab's own in-memory copy now carries the invalid key. In F12 → Application, see how the value of the stored session key (`tezgah.session`) changes between tabs. What does the second tab not know?

## İpucu 3

Müşteri sitesinde oturumu `frontend/apps/storefront/src/auth/tokenStore.ts` tutuyor; bellekteki `current` değeri ortak depodan yalnızca açılışta okunuyor. `frontend/apps/storefront/src/api/client.ts` içindeki `refreshTokens` ise yenileme başarısız olunca doğrudan `tokenStore.clear()` çağırıp **ortak depoyu da siliyor**: bu, öbür sekmenin yazdığı geçerli oturumu da siler ve iki sekme birden çıkış yapar. Önceki sürümde bu iki yerde ne vardı? `git log -p -- frontend/apps/storefront/src/auth/tokenStore.ts frontend/apps/storefront/src/api/client.ts` çıktısında kaldırılan satırlara bakın.

*EN:* On the storefront the session is held by `frontend/apps/storefront/src/auth/tokenStore.ts`; the in-memory `current` value is read from the shared store only at startup. `refreshTokens` in `frontend/apps/storefront/src/api/client.ts`, when the refresh fails, calls `tokenStore.clear()` directly and so **also deletes the shared store**: this wipes the valid session the other tab wrote, and both tabs log out. What was in these two places in the previous version? Look at the removed lines in the output of `git log -p -- frontend/apps/storefront/src/auth/tokenStore.ts frontend/apps/storefront/src/api/client.ts`.
