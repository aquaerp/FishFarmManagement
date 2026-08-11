# تقرير قابلية إعادة إنتاج الإصدار G2

**التاريخ:** 11 أغسطس 2026  
**الفرع:** `codex/commercial-release-baseline`  
**آخر commit مختبر:** `6a8c076`

## نتائج البناء والتحقق

| الفحص | النتيجة |
|---|---|
| Restore locked | ناجح |
| Release build | ناجح، 0 تحذيرات و0 أخطاء |
| الاختبارات | 147 ناجح، 0 فاشل، 0 متجاوز |
| سياسة Release | ناجحة؛ لا توجد هويات Demo في assembly |
| Self-contained publish | ناجح لـ`win-x64` |
| Manifest | تم إنشاؤه مع hash لكل ملف |
| ZIP checksum | تم إنشاؤه |
| مقارنة التشغيلين | متطابقان byte-for-byte |

## دليل الحتمية النهائي

- الإصدار: `1.0.0`.
- اسم الحزمة: `AquaFarmPro-1.0.0-win-x64.zip`.
- SHA-256 للتشغيل الأول: `8b378fc6cd3a485a70cca86223a4183718d58876692a2ad0ca3341747a339af8`.
- SHA-256 للتشغيل الثاني: `8b378fc6cd3a485a70cca86223a4183718d58876692a2ad0ca3341747a339af8`.
- النتيجة: متطابقان.
- مجلد الدليل المحلي المؤقت: `AquaFarm-G2-wrapper`.

## أدلة GitHub Actions المتتالية

| التشغيل | Commit | Run | النتيجة |
|---:|---|---|---|
| 1 | `c01b89d` | [31537041673](https://github.com/aquaerp/FishFarmManagement/actions/runs/31537041673) | ناجح |
| 2 | `122d4f2` | [31537567902](https://github.com/aquaerp/FishFarmManagement/actions/runs/31537567902) | ناجح |
| 3 | `d4d13ba` | [31538198895](https://github.com/aquaerp/FishFarmManagement/actions/runs/31538198895) | ناجح |
| 4 | `c46bf54` | [31538587909](https://github.com/aquaerp/FishFarmManagement/actions/runs/31538587909) | ناجح |
| 5 | `e8e035d` | [31539101098](https://github.com/aquaerp/FishFarmManagement/actions/runs/31539101098) | ناجح |

كل تشغيل اجتاز `restore/build/test`، تدقيق NuGet، فحص الأسرار، سياسة Release، مقارنة حزمتين حتميتين، ورفع artifacts. وبذلك أغلقت بوابة G2 تنظيمياً وهندسياً.

## متطلبات المراحل اللاحقة

يجب توفير شهادة code-signing قبل توزيع الحزمة خارج بيئة Pilot داخلية.
