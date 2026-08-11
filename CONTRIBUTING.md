# Katkı Rehberi

## Branch akışı

1. `develop` dalını güncelleyin.
2. Issue numarasını içeren kısa ömürlü bir branch açın:
   `feature/42-task-create-endpoint`.
3. Küçük ve anlamlı commit'ler oluşturun.
4. Pull request'i `develop` dalına açın ve ilgili issue'yu bağlayın.
5. En az bir onay ve başarılı kontrollerden sonra squash merge kullanın.

## Branch adları

- `feature/<issue>-<kisa-ad>`
- `bugfix/<issue>-<kisa-ad>`
- `hotfix/<issue>-<kisa-ad>`
- `docs/<issue>-<kisa-ad>`
- `test/<issue>-<kisa-ad>`

## Commit biçimi

Conventional Commits kullanılır:

- `feat(api): add task creation endpoint`
- `fix(auth): reject inactive users`
- `docs(api): update project contract`
- `test(tasks): cover invalid status transition`

## Pull request kabul koşulları

- İlgili issue ve kabul kriterleri bağlıdır.
- Derleme ve testler başarılıdır.
- Gizli bilgi veya üretilmiş çıktı eklenmemiştir.
- API değişikliği varsa OpenAPI taslağı güncellenmiştir.
- İş kuralı değişikliği test ile güvence altındadır.
