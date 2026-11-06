# AEHotel tur kataloqu · 6 noyabr 2026

**Qərar:** LocalDB üzərindəki `AEHotelDb.dbo.Tours` cədvəlindəki 19 qeyd təsdiqlənmiş siyahıya tam uyğundur. Hamısı mövcud ID-si ilə saxlanmalıdır. Artıq tur, arxivləmə namizədi və silinmə namizədi sayı **0**-dır.

| Siyahı № | DB ID | Tur | Rezervasiya sayı | Hazırkı ImageFile |
|---:|---:|---|---:|---|
| 1 | 1 | Bakı Şəhər Turu | 2 | hotelimg6.jfif |
| 2 | 2 | Qəbələ Təbiət Turu | 0 | hotelimg7.jfif |
| 3 | 3 | Şəki Mədəniyyət Turu | 2 | hotelimg1.jfif |
| 4 | 8 | Antalya Sahil Turu | 1 | hotelimg4.jfif |
| 5 | 9 | Bakı Tarixi Tur | 0 | hotelimg5.jfif |
| 6 | 10 | Qəbələ Təbiət Macərası | 0 | hotelimg1.jfif |
| 7 | 11 | Şəki Mədəniyyət Turu | 0 | hotelimg1.jfif |
| 8 | 12 | İstanbul İki Qitə Turu | 0 | hotelimg4.jfif |
| 9 | 13 | Antalya Təbiət Macərası | 0 | hotelimg2.jfif |
| 10 | 14 | Kapadokya Hava Şarı | 0 | hotelimg2.jfif |
| 11 | 15 | Dubay Lüks Şəhər Turu | 0 | hotelimg6.jfif |
| 12 | 16 | Əbu-Dabi Mədəniyyət Turu | 0 | hotelimg4.jfif |
| 13 | 17 | Paris Romantik Tur | 0 | hotelimg2.jfif |
| 14 | 18 | Nitsa French Riviera | 0 | hotelimg6.jfif |
| 15 | 19 | Roma Qədim İmperiya | 0 | hotelimg2.jfif |
| 16 | 20 | Milan Moda Paytaxtı | 0 | hotelimg2.jfif |
| 17 | 21 | Maldiv Tropik Cənnət | 0 | hotelimg4.jfif |
| 18 | 22 | Maafushi Ada Macərası | 0 | hotelimg3.jfif |
| 19 | 23 | Vaadhoo Parlayan Plaj | 0 | hotelimg3.jfif |

**Rezervasiyalar.** SQL Server metadatasına görə Tours-a yönələn xarici açar `dbo.Bookings.TourId`-dir. Silmə qaydası `NO_ACTION`-dır. Saylar bütün statuslar daxil edilməklə hesablanıb. Cəmi 5 əlaqə var. TourId-si boş olan tour tipli booking sayı 0-dır. Rezervasiyası olmayan 16 turun hamısı təsdiqlənmiş siyahıdadır.

**Backup.** AEHotelDb üçün yeni tam `COPY_ONLY, CHECKSUM` backup yaradıldı. `RESTORE VERIFYONLY WITH CHECKSUM` uğurlu oldu. Yoxlama işlək bazaya bərpa əməliyyatı aparmadan icra edildi. [Backup faylı](<C:/Users/Aytac's Noteboob/AEHotelDb-before-tour-review-20261106-9C6CD074-B8BB-4947-9B96-8325B70BA5B6.bak>)

**Admin və müştəri səhifələri.** AdminTours komponenti listTours(), Tours komponenti getTours() ilə eyni `GET /api/tours` mənbəyindən oxuyur. İstifadəçi filtrləri müştəri siyahısını daralda bilər. İşləyən lokal API filtrsiz sorğuya bazadakı eyni 19 ID-ni qaytardı. Bu yoxlama mənbə kodu və HTTP cavabı üzrə aparıldı.

**Seed.** Mövcud SeedToursAsync cədvəldə ən azı bir tur olduqda tur əlavə etmədən çıxır. Bu 19 qeyd saxlanıldığı müddətdə həmin seed normal yenidən başlatmada köhnə turları yaratmır. Təsdiq mərhələsində tətbiqi yenidən başlatmağa və ya migration işlətməyə ehtiyac yoxdur.

**Şəkillər üçün dəyişiklik planı**

1. Yoxlanılan React layihəsində `public/tour-images` qovluğu yoxdur. 19 turun hamısında hazırda `hotelimg*.jfif` saxlanılır. Əsl şəkil qovluğunun yolunu müəyyən etmək və faylları yoxlamaq lazımdır.
2. Hər mövcud ID üçün cari ImageFile → təklif olunan fayl cədvəlini hazırlayıb təsdiqə təqdim etmək. İki Şəki turunu ID üzrə ayrıca göstərmək.
3. Təsdiqdən sonra yoxlanmış fayllar frontend-in public/tour-images qovluğundan təqdim edilməlidir. DB-də modelə uyğun veb yolu `/tour-images/<faktiki-fayl-adı>` formatında saxlanmalıdır. Eyni şəkil iki tur üçün təsdiqlənərsə, müştəri səhifəsində uniqueListImages() funksiyasının ikinci şəkli əvəz etməsinin qarşısı alınmalıdır.
4. Yazmadan əvvəl ID, ad, cari ImageFile və rezervasiya saylarını təkrar yoxlamaq. Fərq varsa planı yenidən təqdim etmək. Razılaşdırılmış ID-lərin yalnız ImageFile sütununu transaction daxilində dəyişmək. Silmə, arxivləmə və sxem dəyişikliyi nəzərdə tutulmur.
5. Dəyişiklikdən sonra 19 ID-ni, rezervasiya əlaqələrini, API-də imageFile dəyərlərini və hər faktiki şəkil URL-sinin işləməsini yoxlamaq. Admin və müştəri səhifələrində filtrsiz siyahını müqayisə etmək.

Bu mərhələdə yalnız audit, backup və hesabat hazırlanıb. DELETE, UPDATE və migration icra edilməyib.
