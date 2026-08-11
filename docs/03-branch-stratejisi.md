# Branch Stratejisi

## Depo kararı

Backend, React ve mobil uygulama ilk 30 günlük çalışma boyunca tek bir monorepo
altında tutulacaktır. Bu seçim ortak dokümantasyon, issue takibi ve sürümleme
işini sadeleştirir.

Planlanan uzak depo adı: `team-task-manager`

## Kalıcı branch'ler

- `main`: yalnızca yayınlanabilir ve demo edilebilir sürümler.
- `develop`: günlük entegrasyon dalı; özellik PR'larının hedefi.

## Kısa ömürlü branch'ler

- `feature/*`: yeni özellik
- `bugfix/*`: geliştirme sırasında hata düzeltme
- `hotfix/*`: main üzerinde acil düzeltme
- `docs/*`: yalnızca dokümantasyon
- `test/*`: test kapsamı çalışmaları

## Birleştirme politikası

- `main` ve `develop` doğrudan push'a kapatılır.
- Pull request ve en az bir inceleme zorunludur.
- `develop` hedefinde squash merge kullanılır.
- `develop` → `main` sürüm PR'ında merge commit kullanılır.
- Silinen branch'ler uzak depodan otomatik temizlenir.
- `main` sürümleri `v0.1.0`, `v0.2.0` biçiminde etiketlenir.
