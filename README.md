# Ekip ve Görev Yönetim Sistemi

ASP.NET Core Web API, React + TypeScript ve Expo/React Native istemcilerinden oluşan kurumsal ekip-görev yönetimi örneğidir. JWT/rol yetkilendirmesi, proje ve üyelik, görev, yorum, dosya eki, bildirim ve dashboard akışlarını içerir.

## Solution yapısı

- `TeamTaskManager.Domain`: entity ve enumlar
- `TeamTaskManager.Application`: DTO, sözleşme ve test edilebilir iş kuralları
- `TeamTaskManager.Infrastructure`: EF Core/Identity, SQL Server ve servis uygulamaları
- `TeamTaskManager.Api`: controller, JWT, Swagger ve hata middleware'i
- `TeamTaskManager.Tests`: backend unit testleri
- `src/frontend`: React + TypeScript/Vite web uygulaması
- `src/mobile`: Expo + React Native + TypeScript mobil uygulaması

## En hızlı kurulum (Docker)

1. `.env.example` dosyasını `.env` adıyla kopyalayın ve iki parolayı değiştirin.
2. `docker compose up --build` çalıştırın.
3. Web: `http://localhost:5173`; Swagger: `http://localhost:5073/swagger`; health: `http://localhost:5073/health`.

Development seed hesabı: `admin@teamtaskmanager.local` / `Admin123!`. Bu bilgi yalnızca yerel demo içindir.

## Yerel geliştirme

SQL Server bağlantısını `ConnectionStrings__DefaultConnection`, JWT anahtarını `Jwt__Key` ortam değişkeniyle verin.

```powershell
dotnet tool restore
dotnet ef database update --project src/backend/TeamTaskManager.Infrastructure --startup-project src/backend/TeamTaskManager.Api
dotnet run --project src/backend/TeamTaskManager.Api

cd src/frontend
npm ci
npm run build
npm run dev
```

Yerel API profili varsayılan olarak `http://localhost:5056` açılır. Web için `VITE_API_URL=http://localhost:5056` ayarlayın. Mobil kurulum için [mobil yönergesine](src/mobile/README.md) bakın.

## Test ve API dokümanı

```powershell
dotnet test TeamTaskManager.sln
```

Swagger geliştirme ortamında `/swagger` yolundadır. Postman koleksiyonu `docs/postman/TeamTaskManager.postman_collection.json`, manuel kontroller `docs/TEST-CHECKLIST.md`, sunum konuşma/demo akışı `docs/DEMO-SCRIPT.md` altındadır.

Postman'de environment ve collection dosyalarını içe aktarın, dosya yükleme çalışma dizinini depo kökü olarak seçin ve koleksiyonu 01–17 sırasıyla çalıştırın. Akış; hatalı/doğru giriş, kullanıcı, proje, üyelik, görev CRUD, filtre-sayfalama-sıralama, durum, yorum, dosya, bildirim, dashboard ve rol bazlı 403 kontrolünü kapsar.

## Migration ve örnek veri

Migration'lar `src/backend/TeamTaskManager.Infrastructure/Persistence/Migrations` altındadır. API Development ortamında roller ile demo admin kullanıcısını idempotent olarak oluşturur. Docker yapılandırması migration'ları başlangıçta uygular.

## GitHub ve commit geçmişi

Bu yerel depoda henüz GitHub remote adresi tanımlı değildir; bu nedenle gerçek olmayan bir bağlantı yazılmamıştır. Yayınladıktan sonra `git remote add origin <depo-url>` ve `git push -u origin main` kullanın. Anlamlı Conventional Commit örnekleri: `feat(api): add task status rules`, `test(api): cover project rules`, `docs: add demo checklist`.

## Bilinen eksikler

- Refresh token, SignalR/push bildirim ve çevrimdışı senkronizasyon opsiyonel kapsamda bırakıldı.
- Web TypeScript geçişinde büyük mevcut ekran bileşeni `@ts-nocheck` ile korunuyor; yeni modüller strict TypeScript yazılmalıdır.
- Fiziksel cihaz, iOS ve Docker uçtan uca testi hedef ortamda ayrıca uygulanmalıdır.

## Doğrulanan build'ler

- Backend: Release `dotnet publish` başarılı.
- Unit test: 16/16 başarılı.
- Web: TypeScript kontrolü ve Vite production build başarılı.
- Mobil: TypeScript kontrolü ve Expo Android bundle export başarılı.
- Docker Compose yapılandırması geçerli; bu makinede Docker daemon kapalı olduğundan imaj build'i başlatılamadı.
