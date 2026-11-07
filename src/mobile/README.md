# Mobil uygulama

Expo + React Native + TypeScript istemcisidir. Android emülatörü için `.env.example` dosyasını `.env` olarak kopyalayın; fiziksel cihazda `EXPO_PUBLIC_API_URL` değerini bilgisayarın yerel ağ IP adresiyle değiştirin.

```powershell
cd src/mobile
npm install
npm run android
```

API çalışırken uygulama; giriş, özet, yenilenebilir Görevlerim listesi, görev detayı, durum güncelleme, yorum, 5 MB sınırı ve izinli MIME türleriyle dosya yükleme, bildirimleri okuma ve güvenli çıkış akışlarını sağlar. iOS için macOS üzerinde `npm run ios`; hızlı tarayıcı kontrolü için `npm run web` kullanılabilir.
