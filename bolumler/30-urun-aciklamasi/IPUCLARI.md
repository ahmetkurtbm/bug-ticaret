# İpuçları · Bölüm 30

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Önce uyarı penceresini neyin tetiklediğini bulun. Müşteri sitesinde ürün sayfasını açıp F12 → Elements sekmesinde "Ürün açıklaması" bölümünü inceleyin: açıklama sayfaya nasıl yerleşmiş? Yazdığınız metin ekranda yazı olarak mı görünüyor, yoksa tarayıcı onu sayfanın gerçek bir parçası gibi mi işlemiş? Aynı metni geçen haftaki sürümde nasıl göründüğüyle karşılaştırın.

*EN:* First find what triggers the alert. Open the product page on the storefront and inspect the "Ürün açıklaması" ("Product description") section in F12 → Elements: how was the description placed in the page? Does the text you wrote appear on screen as text, or has the browser treated it as a real part of the page? Compare it with how the same text looked in last week's version.

## İpucu 2

Ürün sayfasını çizen bileşen `frontend/apps/storefront/src/pages/ProductPage.tsx`. Açıklamanın basıldığı yeri bulun ve React'in bir değeri sayfaya normalde nasıl yerleştirdiğiyle, bu satırda yerleştirme biçimiyle karşılaştırın. React normalde metni **güvenli** biçimde basar; bu satırda ise bilerek güvenliği kapatan bir özellik kullanılmış olmalı. Bir satıcının yazdığı metne bu kadar güvenmenin nedeni commit mesajında yazıyor: `git log -p -- frontend/apps/storefront/src/pages/ProductPage.tsx`.

*EN:* The component that draws the product page is `frontend/apps/storefront/src/pages/ProductPage.tsx`. Find where the description is printed and compare how React normally places a value in the page with how it is placed on this line. React normally prints text **safely**; on this line a feature that deliberately turns that safety off must be in use. The reason for trusting a seller's text this much is in the commit message: `git log -p -- frontend/apps/storefront/src/pages/ProductPage.tsx`.

## İpucu 3

`ProductView` içinde açıklama `dangerouslySetInnerHTML={{ __html: product.description }}` ile basılıyor: satıcının yazdığı her şey sayfanın gerçek içeriği oluyor, yani `<img onerror=...>` gibi bir etiket çalışabiliyor. Önceki sürüm açıklamayı etiketlerden arındırıp düz metin olarak basıyordu. Commit mesajındaki "ürün değişiklikleri zaten yönetici onayından geçiyor" gerekçesi de eksik: yönetici onaylarken yalnızca ekranda görünen metne bakıyor; görünmeyen bir etiketi fark etmesi beklenemez. Siz de düşünün: bu sayfayı açan bir müşterinin oturum bilgisi nerede duruyor ve böyle bir kod ona ulaşabilir mi?

*EN:* In `ProductView` the description is printed with `dangerouslySetInnerHTML={{ __html: product.description }}`: everything the seller wrote becomes real content of the page, so a tag like `<img onerror=...>` can run. The previous version stripped the tags and printed the description as plain text. The reasoning in the commit message, "product changes already go through admin approval", is also incomplete: when approving, the admin looks only at the text shown on screen and cannot be expected to notice an invisible tag. Think about it too: where is the session information of a customer who opens this page stored, and can such code reach it?
