# YangiAlifbo (UzbekScriptEngine)

**YangiAlifbo** — Windows uchun real vaqtda ishlaydigan yangi oʻzbek lotin alifbosi transliterator tizimi. Dastur tizim patnisida (system tray) jimgina ishlaydi, matn kiritilishini kuzatib boradi va soʻz ajratgichlar (probel, enter, tinish belgilari) bosilganda 1995-yilgi lotin alifbosidagi soʻzlarni 2024-yilgi isloh qilingan alifboga avtomatik ravishda oʻzgartiradi.

---

## 🎯 Asosiy Imkoniyatlar (Key Features)

### 1. 2024 Yangi Alifbo Qoidalari
* **Oʻ / oʻ** (`o'`, `o‘`, `o’`, `oʻ`, `oʼ`, `o```) $\rightarrow$ **Ö / ö** (*o'rtoq* $\rightarrow$ *örtoq*)
* **Gʻ / gʻ** (`g'`, `g‘`, `g’`, `gʻ`, `gʼ`, `g```) $\rightarrow$ **Ğ / ğ** (*g'alaba* $\rightarrow$ *ğalaba*)
* **Sh / sh** $\rightarrow$ **Ş / ş** (*shahar* $\rightarrow$ *şahar*)
* **Ch / ch** $\rightarrow$ **Ç / ç** (*choyxona* $\rightarrow$ *çoyxona*)

### 2. Katta-Kichik Harflarni Saqlash (Case Preservation)
* `shahar` $\rightarrow$ `şahar` (barchasi kichik)
* `Shahar` $\rightarrow$ `Şahar` (bosh harf katta)
* `SHAHAR` $\rightarrow$ `ŞAHAR` (barchasi katta)
* `o'rtoq` $\rightarrow$ `örtoq` | `O'rtoq` $\rightarrow$ `Örtoq` | `O'RTOQ` $\rightarrow$ `ÖRTOQ`

### 3. Tutuq Belgisi va Boʻgʻin Ajratish Istisnolari
* Tutuq belgisi bilan ajratilgan morfemalardagi `s` va `h` harflari **hech qachon** `ş` ga aylanmaydi:
  * `Is'hoq` $\rightarrow$ `Is'hoq`
  * `mus'haf` $\rightarrow$ `mus'haf`
  * `as-hob` $\rightarrow$ `as-hob`
  * `mas'ul` $\rightarrow$ `mas'ul`

### 4. 3 Bosqichli Til Ajratish Dvigateli (Linguistic Discrimination Engine)
Boshqa tillardagi soʻzlar (ingliz, rus, dasturlash kalit soʻzlari) buzilib ketmasligi kafolatlangan:
* **1-daraja (Fast Reject Filter)**: 10,000+ eng koʻp ishlatiladigan inglizcha va dasturlash soʻzlari (`show`, `crash`, `child`, `watch`, `switch`, `match`, `fetch`, `catch`, `shell`, `shift`, `char`, `echo`) darhol inkor qilinadi.
* **2-daraja (Uzbek Lexicon)**: 33,000+ dan ortiq rasmiy oʻzbek soʻzlari va oʻzaklari tekshiriladi.
* **3-daraja (Agglutinative Morphology)**: Oʻzbek tilidagi qoʻshimchalar (`-lar`, `-ning`, `-ga`, `-dan`, `-im`, `-moq`, `-yapti` va h.k.) morfologik tekshiruvdan oʻtkaziladi.
* **Foydalanuvchi Roʻyxatlari**: `%AppData%\YangiAlifbo\user_whitelist.txt` va `user_blacklist.txt` orqali shaxsiy sozlamalar kiritish mumkin.

### 5. Xavfsiz Win32 Integratsiyasi
* `WH_KEYBOARD_LL` quyi darajadagi klaviatura ilmogʻi.
* `SendInput` orqali kiritishda `dwExtraInfo = 0x59414E47` ("YANG") belgisi bilan cheksiz rekursiyadan 100% himoyalangan.
* Buyruqlar satri (Terminal, PowerShell, CMD, VS Code) va parol kiritish maydonlarida avtomatik oʻchadi.

---

## ⌨️ Boshqaruv va Tezkor Tugmalar

* **Global Tezkor Tugma**: `Ctrl + Shift + U` — Konvertor dvigatelini bir zumda yoqish / oʻchirish.
* **System Tray Belgi**:
  * Yashil doira (Ö) = Tizim faol va ishlamoqda.
  * Kulrang doira (Ö) = Tizim vaqtincha toʻxtatilgan.
* **Sichqonchaning oʻng tugmasi menyusi**:
  * Yoqilgan / Oʻchirilgan
  * Rejim: Qatʼiy (Lugʻat asosida) / Agressiv
  * Oq roʻyxat / Qora roʻyxatni tahrirlash
  * Windows bilan birga ishga tushirish (Run on Startup)
  * Dastur haqida va Chiqish

---

## 🛠 Qurish va Ishga Tushirish (Build & Run)

### Talablar
* Windows 10 yoki 11 (x64)
* .NET 10 SDK

### Loyihani yigʻish va testlash
```powershell
# 1. Testlarni yurgizish
dotnet test tests/YangiAlifbo.Tests.csproj

# 2. Toʻliq yagona fayl (single-file executable) yaratish
.\publish.ps1
```
Natijada `publish/YangiAlifbo.exe` oʻz-oʻzini taʼminlovchi (self-contained, ~15MB) dastur hosil boʻladi.
