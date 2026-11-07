# Kurulum dokümanı

Gereksinimler: .NET 10 SDK, Node.js 24+, npm, SQL Server 2022 veya Docker Desktop. Docker yolu önerilir: kökte `.env.example` → `.env`, ardından `docker compose up --build`.

Yerel çalıştırmada önce SQL Server bağlantısını ortam değişkeni olarak tanımlayın, migration komutunu README'deki biçimde çalıştırın, sonra API/web/mobil istemcilerini ayrı terminallerde başlatın. Android emülatörü host makineye `10.0.2.2` üzerinden ulaşır; fiziksel cihazda yerel ağ IP'si kullanılmalıdır.

Sorun giderme: 401 için token/seed hesabını, 403 için rol ve proje üyeliğini, CORS için istemci origin'ini, SQL hatalarında connection string ile SQL Server durumunu kontrol edin.
