# İpuçları · Bölüm 11 (FE-03)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Önce sunucuya ne gittiğine bakın: tarayıcıda Network sekmesini açın, `+` düğmesine 4 kez basın ve giden isteğin gövdesindeki adedi okuyun. Sonra `src/cart` klasöründeki adet düğmelerini yöneten kodu bulun ve tıklamayı karşılayan fonksiyonun ilk satırına `console.log` koyun. VS Code'da "Müşteri sitesi (Chrome)" yapılandırmasıyla aynı satıra kesme noktası da koyabilirsiniz. Her tıklamada fonksiyonun hangi değerleri gördüğünü yazdırın.

*EN:* First look at what is sent to the server: open the Network tab, press `+` four times and read the quantity in the request body. Then find the code that drives the quantity buttons in the `src/cart` folder and put a `console.log` on the first line of the function that handles the click. In VS Code you can also set a breakpoint on that line with the "Müşteri sitesi (Chrome)" launch configuration. Print which values the function sees on each click.

## İpucu 2

Ekrandaki adet doğru artıyor, ama sunucuya giden yanlış. Demek ki iki değer farklı yerden okunuyor: biri çizimde kullanılan güncel state, öteki tıklama fonksiyonunun içinden görülen değer. Bir fonksiyonun hangi render'ın değişkenlerini gördüğü, fonksiyonun ne zaman oluşturulduğuna bağlıdır. `useCallback`'in ikinci argümanı ne işe yarar ve burada ne yazıyor, düşünün. Dosyada susturulmuş bir lint uyarısı da var.

*EN:* The quantity on screen goes up correctly, but what is sent to the server is wrong. So two values are read from different places: the current state used for rendering, and the value seen from inside the click function. Which render's variables a function sees depends on when the function was created. Think about what the second argument of `useCallback` does and what it says here. There is also a silenced lint warning in the file.

## İpucu 3

`frontend/apps/storefront/src/cart/useQuantityStepper.ts` dosyasında `change` fonksiyonu `base` değerini `pending ?? serverQuantity` ile state'ten okuyor. Ama `useCallback` bağımlılık listesi yalnızca `[max]`; fonksiyon bir kez oluşuyor ve hep ilk çizimdeki `pending` (`null`) ve `serverQuantity` değerini görüyor. Bu yüzden her tıklama aynı başlangıç adedinden hesaplıyor. Aynı dosyada zamanlayıcının güncel adedi okuduğu başka bir yer var; tıklamanın da güncel adedi oradan okuması gerekir. Satırın sonundaki `eslint-disable` yorumu da bu uyarıyı kapatmış.

*EN:* In `frontend/apps/storefront/src/cart/useQuantityStepper.ts`, the `change` function reads `base` from state with `pending ?? serverQuantity`. But the `useCallback` dependency list is only `[max]`; the function is created once and always sees the first render's `pending` (`null`) and `serverQuantity`. So every click computes from the same starting quantity. Elsewhere in the same file there is a place where the timer reads the current quantity; the click should read the current quantity from there too. The `eslint-disable` comment at the end of the line has silenced this very warning.
