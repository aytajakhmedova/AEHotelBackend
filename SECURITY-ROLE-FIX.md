AEHotel Admin / SuperAdmin təhlükəsizlik düzəlişi — 4 noyabr 2026

Hər iki layihə tapıldı və düzəlişlər tətbiq edildi. Adi Admin SuperAdmin-i siyahıda görə bilir, lakin onu redaktə, silmə, deaktiv etmə, parolunu və rolunu dəyişmə əməliyyatları backend-də 403 ilə bloklanır. SuperAdmin rolunu yaratma zamanı və mövcud hesaba vermə zamanı da eyni qayda işləyir.

İşləyən API prosesi yenidən başladılmayıb. Yeni backend kodunun həmin prosesdə qüvvəyə minməsi üçün API yenidən build/start edilməlidir. Testlər istehsal tətbiqinin Program.cs faylını işlətməyib; onun avtomatik migration və seeder-ləri başladılmayıb.

**Dəyişikliklərin məzmunu**

- AdminStaff: qorunan sətirdə Redaktə/Sil gizlədilir; redaktəni açma, yadda saxlama və silmə handler-ləri ayrıca yoxlanır. Yadda saxlama zamanı hədəfin ilkin server məlumatı yoxlanır.
- AdminUsers: SuperAdmin üçün Admin-ə rol/otel idarəetməsi təqdim edilmir. Çoxrollu istifadəçidə SuperAdmin prioritet sayılır. Hər iki səhifədə SuperAdmin öz icazəli rol seçimlərini saxlayır.
- Auth helper: rol, hesab identifikatoru, email və hotel scope JWT payload-dan götürülür; localStorage.user.roles icazə mənbəyi deyil. Korlanmış və vaxtı keçmiş tokenlə rol icazəsi verilmir. Brauzerdə JWT oxunması imzanın yoxlanması deyil: imzanı mövcud backend JwtAuthHandler yoxlayır.
- UserManagementPolicy: əməliyyatı edən şəxsin aktivliyi və cari rolları, hədəfin cari rolları Identity vasitəsilə bazadan yoxlanır. Köhnə JWT-də SuperAdmin yazılsa da, bazada səlahiyyəti götürülmüş və ya deaktiv edilmiş hesab bu əməliyyatları edə bilmir.
- Sonuncu aktiv SuperAdmin həm staff, həm də ayrıca users/{id}/role yolunda qorunur. Öz hesabını silmə/deaktiv etmə üzrə mövcud qadağalar saxlanılıb; özünə yeni rol vermə yolu da bloklanıb.
- İcazə yoxlaması və yazmalar eyni Serializable transaction daxilindədir. Aralıq uğursuzluqda profil/status/rol dəyişiklikləri rollback edilir. Mövcud Identity nəticə yoxlamaları qorunub.
- AppRoles, JWT autentifikasiya handler-i və hotel scope helper-ləri analiz edildi, dəyişdirilmədi. API URL-ləri və uğurlu response formaları dəyişdirilmədi. Home/Header/Footer, CSS, rezervasiya, uçuş, tur və ödəniş məntiqinə toxunulmadı. Migration yaradılmadı, real istifadəçi silinmədi və rolları dəyişdirilmədi.

**Build nəticələri**

| Əmr/yoxlama | Nəticə |
| --- | --- |
| dotnet build AEHotel.sln -c Release --no-restore --nologo -p:UseAppHost=false | Uğurlu: 0 xəta, 7 mövcud xəbərdarlıq |
| İlk Debug build | İşləyən API-nin kilidlədiyi DLL-lərə görə uğursuz; API prosesi dayandırılmadı |
| npm run build | Uğursuz: əvvəlcədən mövcud Header.css sintaksis xətası |
| Əvvəlki frontend faylları ilə yaddaşdaxili build | Eyni CSS xətası təkrarlandı |
| Cari frontend, cssMinify:false, write:false | Uğurlu diaqnostik build; konfiqurasiya dəyişdirilmədi |

Frontend build maneəsi [Header.css](<C:/Users/Aytac's Noteboob/Desktop/FinalProject/client/hotelreservation/src/components/Header/Header.css:263>) faylındakı selektorsuz CSS deklarasiyalarıdır. Bu tapşırıqda həmin fayl dəyişdirilmədi. Backend xəbərdarlıqları mövcud MailKit/MimeKit/System.Security.Cryptography.Pkcs paket bildirişləri və IdentitySeeder.cs:107 nullable xəbərdarlığıdır.

**Həqiqətən icra edilmiş testlər**

Son tam backend test icrası: `dotnet test AEHotel.sln -c Release --no-build --no-restore --nologo --logger "trx;LogFileName=security.trx"` — **34 keçdi, 0 uğursuz, 0 buraxıldı**. Build-li test icrası da aparılıb. [TRX nəticəsi](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.SecurityTests/TestResults/security.trx>).

TestServer real controller, servis, Identity store, JWT handler və exception middleware ilə işləyir. Hər test ayrıca SQLite :memory: bazası yaradır. Production connection string, migration və seed istifadə edilmir.

| Ssenari | Nəticə |
| --- | --- |
| Admin → SuperAdmin redaktə, parol dəyişmə, deaktiv etmə, silmə | HTTP 403; hədəfin məlumatları dəyişməyib |
| Admin → SuperAdmin rolunu alma/verme, eyni rolu yenidən təyin etmə | HTTP 403 |
| Admin → özünü SuperAdmin etmək, başqa işçini yüksəltmək, SuperAdmin yaratmaq | HTTP 403; yeni hesab yaranmayıb |
| Hədəfdə Admin + SuperAdmin birlikdədir | SuperAdmin qoruması işləyir |
| SuperAdmin → icazəli redaktə/deaktiv etmə/silmə/rol vermə/rol alma | Müvafiq 200, 201 və 204 |
| Sonuncu aktiv SuperAdmin; başqa SuperAdmin deaktivdir | Silmə, deaktiv etmə və rol alma bloklanır |
| Köhnə SuperAdmin JWT, bazada rol/aktivlik dəyişib | HTTP 403 |
| Admin və SuperAdmin → adi işçi yaratma/redaktə/deaktiv/aktiv/silmə | Uğurlu |
| Admin → müştəri rolunun qanuni dəyişdirilməsi | Uğurlu |
| HotelManager/Receptionist/Customer → qlobal istifadəçi idarəetməsi | HTTP 403 |
| HotelManager/Receptionist scope helper-i | Öz otelinə icazə, başqa/naməlum otelə 403 |
| Parol yenilənməsi uğursuz olur | Profil, aktivlik və rol dəyişiklikləri rollback edilir |

Headless Edge + ayrıca Vite serverində **12 frontend yoxlaması keçdi**. Bütün API sorğuları fixture-lərlə əvəz edildi; yazma sorğuları real backend-ə buraxılmadı. Yoxlamalar Admin üçün düymələrin gizlənməsi, adi işçi düymələrinin qalması, SuperAdmin rol seçiminin olmaması, birbaşa /admin/users keçidi, çoxrollu hədəf, saxta localStorage rolu, SuperAdmin idarəetməsi, yararsız/vaxtı keçmiş JWT və digər rolların/anonim istifadəçinin birbaşa keçidlərindən ibarətdir. [Təkrar icra qaydası](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.SecurityTests/README.md>).

**Dəyişən fayllar və diff**

Layihələrdə tapşırıq başlamazdan əvvəl çoxlu dəyişiklik və untracked fayl var idi. Bunlar saxlanıldı. Aşağıdakı [git diff](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/artifacts/security-fix.diff>) tapşırıqdan əvvəlki nüsxələrlə müqayisədir; əvvəlki istifadəçi dəyişikliklərini bu işin nəticəsi kimi göstərmir. Backend/frontend prefiksləri iki layihəni ayırır.

| Fayl | Əlavə / silinən sətir |
| --- | --- |
| [AEHotel.sln](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.sln>) | +14 / -0 |
| [UsersController.cs](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.API/Controllers/UsersController.cs>) | +35 / -25 |
| [AdminController.cs](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.API/Controllers/AdminController.cs>) | +1 / -1 |
| [AdminService.cs](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.Service/Services/AdminService.cs>) | +26 / -24 |
| [IAdminService.cs](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.Service/Services/Interfaces/IAdminService.cs>) | +1 / -1 |
| [DependencyInjection.cs](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.Service/DependencyInjection.cs>) | +2 / -0 |
| [UserManagementPolicy.cs — yeni](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.Service/Security/UserManagementPolicy.cs>) | +45 / -0 |
| [auth.js](<C:/Users/Aytac's Noteboob/Desktop/FinalProject/client/hotelreservation/src/api/auth.js>) | +27 / -2 |
| [AdminStaff.jsx](<C:/Users/Aytac's Noteboob/Desktop/FinalProject/client/hotelreservation/src/pages/Admin/Staff/AdminStaff.jsx>) | +17 / -8 |
| [AdminUsers.jsx](<C:/Users/Aytac's Noteboob/Desktop/FinalProject/client/hotelreservation/src/pages/Admin/Users/AdminUsers.jsx>) | +12 / -4 |
| [AEHotel.SecurityTests.csproj — yeni](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.SecurityTests/AEHotel.SecurityTests.csproj>) | +17 / -0 |
| [SecurityApp.cs — yeni](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.SecurityTests/SecurityApp.cs>) | +127 / -0 |
| [UserManagementTests.cs — yeni](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.SecurityTests/UserManagementTests.cs>) | +221 / -0 |
| [frontend-security.mjs — yeni](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.SecurityTests/frontend-security.mjs>) | +124 / -0 |
| [Test README — yeni](<C:/Users/Aytac's Noteboob/Desktop/AEHotelBackend/AEHotel.SecurityTests/README.md>) | +30 / -0 |

Kod/test diff-i: **15 fayl, +699 / -65**. Bu hesabat ayrıca əlavə edilib. Müvəqqəti nüsxələr, build yoxlama skriptləri və diff artifacts/ daxilindədir. Dəyişdirilən hissələr üçün whitespace yoxlaması keçdi. Reponun ümumi git diff --check nəticəsində bu işdən əvvəl dəyişdirilmiş başqa fayllarda trailing whitespace mövcuddur; həmin fayllara toxunulmadı. Commit yaradılmadı.

**Test edilməmiş risklər və qalan addımlar**

- Canlı SQL Server-də paralel sorğu/deadlock/yük sınağı aparılmadı. Serializable transaction istifadə olunur, lakin SQLite testləri SQL Server-in paralellik davranışını tam əvəz etmir.
- Real hesablarda login-dən başlayaraq canlı sistem üzrə tam sınaq aparılmadı. Backend HTTP testləri və frontend fixture testləri ayrı mühitlərdə icra edildi.
- İşləyən API prosesi köhnə yüklənmiş kodu istifadə edə bilər; təhlükəsizlik düzəlişinin canlı prosesdə qüvvəyə minməsi üçün yenidən başlatma lazımdır. Bu işdə avtomatik migration/seeder işə salınmadı.
- Standart frontend production build yuxarıdakı mövcud CSS xətası həll olunana qədər keçmir. Diaqnostik build standart build-in əvəzi sayılmır.
- Artıq verilmiş JWT-nin rolu müddəti bitənədək UI-da köhnə görünə bilər. Dəyişdirilən backend əməliyyatları cari database rolunu yoxladığı üçün bu, həmin əməliyyatlara icazə vermir. Digər endpoint-lər üzrə ümumi token ləğvi bu işdə dəyişdirilməyib.
