# Swagger / OpenAPI

API, ASP.NET Core Swagger üreticisiyle controller ve DTO'lardan güncel OpenAPI sözleşmesini çalışma anında oluşturur.

- Docker UI: `http://localhost:5073/swagger`
- Docker JSON: `http://localhost:5073/swagger/v1/swagger.json`
- Yerel profil UI: `http://localhost:5056/swagger`

Yetkili endpointleri denemek için önce `/api/auth/login` çağrısından JWT alın ve Swagger **Authorize** alanına `Bearer <token>` yazın. Postman koleksiyonu token'ı login test script'i ile environment'a otomatik kaydeder.
