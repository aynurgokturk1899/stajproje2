# Bilinen eksikler ve sonraki adımlar

- Web'in mevcut büyük ekran bileşeni davranışı korumak amacıyla geçişte `@ts-nocheck` kullanır; yeni kod strict TypeScript'tir.
- Refresh token, SignalR, push bildirim ve çevrimdışı senkronizasyon opsiyonel kapsamdır.
- GitHub remote tanımlı değildir; gerçek depo URL'si yayınlama sonrasında README'ye eklenmelidir.
- Bu çalışma ortamında sunum becerisinin zorunlu `@oai/artifact-tool` paketi eksik olduğundan PPTX export edilemedi. Sunuma hazır zamanlanmış içerik ve canlı demo metni `DEMO-SCRIPT.md` dosyasındadır.
- Docker Compose yapılandırması doğrulandı; yerel Docker daemon kapalı olduğu için imajların bu makinedeki son build/çalıştırma testi uygulanamadı. Backend Release publish, web production build ve Android bundle ayrı ayrı başarıyla üretildi.
