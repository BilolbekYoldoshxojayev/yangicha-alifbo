# YangiAlifbo

[![Sayt](https://img.shields.io/badge/sayt-yangicha--alifbo-13205c)](https://bilolbekyoldoshxojayev.github.io/yangicha-alifbo/)
[![Reliz](https://img.shields.io/github/v/release/BilolbekYoldoshxojayev/yangicha-alifbo?color=f0b23a)](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest)
[![Litsenziya: MIT](https://img.shields.io/badge/litsenziya-MIT-2fb3a6)](LICENSE)

![YangiAlifbo — ş ç ö ğ](docs/assets/img/og.png)

**YangiAlifbo** — Windows uchun tizim patnisida ishlaydigan dastur. Word, Telegram, brauzer yoki bloknotda odatdagidek eski lotin imlosida yozasiz; so‘z tugashi bilan (probel, Enter, tinish belgisi) u yangi alifboga o‘giriladi:

| Eski | Yangi | Misol |
|---|---|---|
| `sh` | `ş` | shahar → şahar |
| `ch` | `ç` | choyxona → çoyxona |
| `o‘` / `o'` / `oʻ` | `ö` | o‘rtoq → örtoq |
| `g‘` / `g'` / `gʻ` | `ğ` | g‘alaba → ğalaba |

Sayt va brauzerdagi demo: **https://bilolbekyoldoshxojayev.github.io/yangicha-alifbo/**

## O‘rnatish

1. [Relizlar](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest) sahifasidan `YangiAlifbo.exe` ni yuklab oling (Windows 10/11, x64).
2. Ishga tushiring. O‘rnatuvchi yo‘q — .NET 10 runtime fayl ichiga joylangan (self-contained, single-file).
3. Odatdagidek yozing.

Fayl raqamli imzolanmagan, shuning uchun Windows SmartScreen ogohlantirish ko‘rsatishi mumkin. Faylni faqat rasmiy relizdan yuklang va SHA-256 qiymatini relizdagi bilan solishtiring:

```powershell
Get-FileHash .\YangiAlifbo.exe -Algorithm SHA256
```

## Foydalanish

- **`Ctrl + Shift + U`** — konvertorni yoqish / o‘chirish.
- Patnisdagi belgi: yashil — faol, kulrang — to‘xtatilgan. O‘ng tugma menyusida: yoqish/o‘chirish, rejim (Qat’iy / Agressiv), oq va qora ro‘yxatni tahrirlash, Windows bilan birga ishga tushirish, dastur haqida, chiqish.
- **Avtomatik ishga tushirish** sukut bo‘yicha yoqilgan (v1.1.0 dan). Menyudan o‘chirsangiz, tanlov saqlanadi.
- **Qat’iy rejim** (standart): faqat lug‘at yoki morfologiya tasdiqlagan so‘zlar o‘zgaradi. **Agressiv rejim**: filtrdan o‘tgan har qanday so‘z o‘zgaradi.
- Shaxsiy ro‘yxatlar: `%AppData%\YangiAlifbo\user_whitelist.txt` va `user_blacklist.txt` (har qatorda bitta so‘z, saqlanganda qayta yuklanadi).

## Qanday ishlaydi

1. **Naqsh.** So‘zda `sh`, `ch`, `o‘`, `g‘` bo‘lmasa, unga tegilmaydi. Tutuq belgisi ajratgan `s'h` / `c'h` (Is’hoq, mus’haf, as’hob) hisobga olinmaydi.
2. **Filtr.** 10 055 ta inglizcha va dasturlash so‘zi (`show`, `push`, `switch`, `catch`…) o‘zgartirilmaydi.
3. **Lug‘at.** 938 526 yozuvli o‘zbek lug‘ati (so‘z va o‘zaklar, eski va yangi imloda).
4. **Morfologiya.** 400+ qo‘shimchani (`-lar`, `-imiz`, `-dan`, `-gan`, `-yapti`…) va nisbat qo‘shimchalarini (`-n`, `-il`, `-t`, `-tir`) ajratib, o‘zak qidiriladi.

Katta-kichik harf saqlanadi (`Shahar → Şahar`, `SHAHAR → ŞAHAR`), apostrofning 9 xil ko‘rinishi (`' ‘ ’ ʻ ʼ ` ´ ‛ ′`) taniladi.

**Xavfsizlik:** terminal va IDE jarayonlarida (cmd, PowerShell, Windows Terminal, VS Code, Visual Studio, Git Bash, WSL va b.) va Windows parol maydoni deb belgilagan joylarda (ES_PASSWORD, UI Automation IsPassword) konvertatsiya o‘chadi. `SendInput` orqali kiritilgan belgilar `dwExtraInfo = 0x59414E47` bilan belgilanadi, shuning uchun hook o‘z kiritishini qayta ishlamaydi. Ilova tarmoqqa ulanmaydi.

**Ma’lum cheklovlar:**
- Administrator huquqi bilan ishlayotgan dasturlarda ishlamaydi (Windows UIPI chegarasi).
- Qat’iy rejimda lug‘atda yo‘q so‘zlar o‘zgarmaydi — masalan, ba’zi atoqli otlar (`Toshkent`). Ularni oq ro‘yxatga qo‘shing yoki xato sifatida xabar bering.

## Loyihani yig‘ish

Talablar: Windows 10/11, [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet test tests/YangiAlifbo.Tests.csproj -c Release        # 394 ta unit test
dotnet test e2e_tests/YangiAlifbo.E2ETests.csproj -c Release  # 270 ta E2E test
.\publish.ps1                                                # testlar + publish/YangiAlifbo.exe
```

Tuzilma:

```
src/Core/       UzbekPhonetics.cs (harf qoidalari), LanguageDetector.cs (filtr, lug‘at, morfologiya)
src/Core/Dictionaries/   english_reject.txt, uzbek_words.txt, uzwords_raw.txt
src/Win32/      klaviatura/sichqoncha hook’lari, fokus va parol maydonini aniqlash, SendInput
src/UI/         tizim patnisi, menyu, Ctrl+Shift+U
tests/, e2e_tests/       MSTest
docs/           GitHub Pages sayti (statik, yig‘ish bosqichi yo‘q)
```

`docs/assets/js/engine.js` — `UzbekPhonetics.cs` ning brauzerdagi nusxasi (demo uchun). U C# dvigatel bilan 5 012 ta so‘zda bir xil natija berishi tekshirilgan. Brauzer demosida lug‘at va morfologiya yo‘q: u harf qoidalari va filtr bilan ishlaydi.

## Hissa qo‘shish

- **Xato:** [Issues](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/issues/new) da so‘z, kutilgan natija va qaysi dasturda yozganingizni yozing.
- **Kod:** repozitoriyani fork qiling, o‘zgartiring, `dotnet test` ikkala loyihada o‘tishini tekshiring va pull request yuboring. Lug‘atga tegishli o‘zgarishlarda yangi so‘zlar uchun test qo‘shing.

## Manbalar

O‘zbek lug‘ati [anvarnarz/uzwords](https://github.com/anvarnarz/uzwords) ro‘yxati asosida `build_dictionaries.py` bilan tuzilgan.

## Litsenziya

[MIT](LICENSE) © 2026 Bilolbek Yoldoshxojayev
