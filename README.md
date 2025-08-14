# NetRoll

Blazor Server alapú alkalmazás kategória-kezeléssel, képkivágó (CropperJS) integrációval és drag & drop rendezéssel (SortableJS).

## Fő funkciók
- Felhasználó / Identity + szerepkörök (Admin, Editor, Viewer)
- Kategóriafák hierarchikus kezelése drag & drop-pal
- Képek feltöltése és kivágása (CropperJS)
- Többnyelvűség (hu / en) erőforrás fájlokkal
- EF Core + SQL Server migrációk automatikus alkalmazása induláskor

## Projekt felépítés
```
NetRoll/
  Components/        Blazor komponensek (oldalak, layout)
  Controllers/       API + MVC controllerek (pl. képfeltöltés)
  Data/              DbContext, migrációk
  Models/            Entitás modellek
  Resources/         Lokalizációs resx fájlok
  Services/          Alkalmazás szolgáltatások (kép storage, email, stb.)
  wwwroot/           Statikus fájlok (moduláris JS: site-core.js, drag-categories.js, cropper.js + site.js aggregator)
```

## Build & futtatás
```powershell
dotnet build
dotnet run
```
Alapértelmezett fejlesztői HTTPS port: 7237

## Első Git push
1. Hozz létre egy üres publikus vagy privát repo-t GitHubon: `NetRoll`
2. Add hozzá origin távoli URL-t és push:
```powershell
git remote add origin https://github.com/<felhasznalo>/NetRoll.git
git branch -M main
git push -u origin main
```
Vagy GitHub CLI-vel (ha telepítve):
```powershell
gh repo create NetRoll --public --source . --remote origin --push
```

## Konfiguráció / Titkok
- `appsettings.Development.json` nem tartalmazzon érzékeny jelszavakat a publikus repo-ban.
- SMTP adatok külön user-secrets-ben vagy környezeti változóként.

## Licence
Adj hozzá egy LICENSE fájlt (pl. MIT), ha nyílttá teszed.

## Következő lépések
- Alap unit tesztek hozzáadása (pl. DisplayOrder rendezés logika)
- CI workflow (GitHub Actions) lint + build + EF migráció teszt
 - Napi automatikus backup workflow (repo tartalmának artefaktba csomagolása)

## Backup workflow
A `.github/workflows/backup.yml` napi 02:00 UTC-kor (és manuálisan) lefut, kitakarítja a fordítási mappákat és feltölti a teljes forráskódot tömörített artefaktként 30 nap megőrzéssel.
Manuális indítás: GitHub → Actions → repo-backup → Run workflow.

