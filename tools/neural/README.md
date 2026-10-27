# Neyron model (4-bosqich)

`src/Core/NeuralFallback.cs` — Qat’iy rejimdagi to‘rtinchi bosqich: lug‘at va morfologiya tanimagan so‘z o‘zbekchami, shuni baholaydigan 5 ta kichik belgi darajasidagi CNN (har biri 124 641 parametr, 8 bitli butun sonlarda saqlanadi, exe’ga ~0,6 MB qo‘shadi). So‘z faqat quyidagilarning hammasi bajarilsa o‘giriladi:

1. So‘z inglizcha filtrda, qora ro‘yxatda yo‘q va lug‘at/morfologiya uni tanimagan.
2. So‘z foydalanuvchi yozgan ko‘rinishda (chat normalizatori o‘zgartirgan shakl modelga berilmaydi).
3. So‘z faqat o‘zbek lotin harflaridan iborat (w yo‘q, c faqat ch tarkibida), 4–32 harf, ichida sh/ch/o‘/g‘ bor.
4. So‘zning barcha 3 harfli birikmalari (boshi va oxiri bilan) o‘quv to‘plamidagi kamida 2 ta o‘zbek so‘zida uchraydi.
5. 5 ta modelning eng past bahosi chegaradan yuqori. Chegara — validatsiya va testdagi 16 714 ta xorijiy so‘z olgan eng yuqori ball.

Model hech qachon lug‘at qabul qilgan so‘zni rad etmaydi. `report.txt` — oxirgi o‘qitish natijalari.

## Qayta o‘qitish

```bash
python -I build_data.py <data_dir> <python site-packages ...>   # ma'lumot (LibreOffice lug'atlari kerak)
python train.py <data_dir> <out_dir> --wneg 12                   # PyTorch + CUDA, RTX 4080 da ~8 daqiqa
python analyze.py <out_dir>                                      # ansambl qoidalarini solishtirish
python quantize.py <out_dir>/uzbek_neural.bin <data_dir> <q_dir>  # int8 + chegarani qayta hisoblash
python pack.py <q_dir>                                           # src/ va tests/ ga joylaydi
```
