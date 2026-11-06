# Otel axtarışı və çoxotaqlı rezervasiya

Aşağıdakı ID və məbləğlər ayrıca SQL Server test bazasında Swagger ilə icra edilmiş ikiotaqlı nümunədəndir. Frontend otaq ID-lərini hər axtarışın cavabından götürür. Tarixlər `YYYY-MM-DD` formatındadır. Giriş günü rezervasiyaya daxildir, çıxış günü növbəti rezervasiyanın giriş günü ola bilər.

## Məkan və mövcudluq

| Metod və yol | İstifadə |
| --- | --- |
| `GET /api/site/cities` | DB-dəki şəhərləri `[{ "name": "...", "country": "..." }]` şəklində qaytarır. |
| `GET /api/site/countries` | DB-dəki ölkə adlarını mətn massivi kimi qaytarır. |
| `GET /api/hotels` | `city` və ya `country`, `checkIn`, `checkOut`, `adults`, `children`, `rooms` ilə otelləri axtarır. |
| `GET /api/hotels/{id}` | Eyni tarixlər və qonaq/otaq sayı ilə həmin oteldə seçimə uyğun otaqları qaytarır. |

```http
GET /api/hotels?checkIn=2030-10-08&checkOut=2030-10-10&adults=2&children=2&rooms=2
GET /api/hotels/2?checkIn=2030-10-08&checkOut=2030-10-10&adults=2&children=2&rooms=2
```

Aktiv və boş otaqlardan tələb olunan sayda seçim birlikdə bütün qonaqları yerləşdirə bilirsə, otel nəticəyə daxil edilir. Detal cavabındakı `rooms` massivinə uyğun kombinasiyada istifadə edilə bilən otaqlar düşür. Siyahıda iki otaq istəyə görə ikidən çox namizəd ola bilər. Frontend konkret otaqlar seçildikdə onların ümumi tutumunu göstərir və dəqiq qiyməti soruşur.

## Qiymət

`POST /api/hotels/2/quote` seçilmiş kombinasiyanı yoxlayır. Eyni sorğu qiymətə baxmaq üçün təkrar göndərilə bilər. Otaqlar yalnız rezervasiya yaradıldıqda tutulur.

```json
{
  "roomIds": [3, 4],
  "rooms": 2,
  "adults": 2,
  "children": 2,
  "checkIn": "2030-10-08",
  "checkOut": "2030-10-10"
}
```

`200 OK` cavabı

```json
{
  "nights": 2,
  "total": 1982.40,
  "rooms": [
    { "roomId": 3, "roomName": "Standart Otaq", "adults": 1, "children": 1, "pricePerNight": 380, "total": 896.80 },
    { "roomId": 4, "roomName": "Deluxe Otaq", "adults": 1, "children": 1, "pricePerNight": 460, "total": 1085.60 }
  ]
}
```

Otaq üzrə məbləğ gecə sayını, mövcud əlavə qonaq tarifini və 18% vergini əhatə edir. Endirim və kupon varsa, rezervasiyanın yekun məbləği mövcud qiymət qaydalarına əsasən hesablanır.

## Rezervasiya

`POST /api/bookings` üçün `Authorization: Bearer <JWT>` tələb olunur. Checkout `expectedStayTotal` sahəsində istifadəçinin təsdiqlədiyi, kupondan əvvəlki otel qiymətini göndərir. Backend otaqları, tarixləri və bu qiyməti tranzaksiya daxilində yenidən yoxlayır.

```json
{
  "bookingType": "hotel",
  "hotelId": 2,
  "roomIds": [3, 4],
  "rooms": 2,
  "adults": 2,
  "children": 2,
  "checkIn": "2030-10-08",
  "checkOut": "2030-10-10",
  "expectedStayTotal": 1982.40,
  "guestName": "Stay QA",
  "guestEmail": "stay-admin@example.test",
  "paymentMethod": "cash"
}
```

`201 Created` cavabından seçilmiş sahələr

```json
{
  "id": 7,
  "hotelId": 2,
  "guests": 4,
  "nights": 2,
  "roomId": 3,
  "roomName": "Standart Otaq, Deluxe Otaq",
  "rooms": [
    { "roomId": 3, "roomName": "Standart Otaq", "adults": 1, "children": 1, "pricePerNight": 380, "total": 896.80 },
    { "roomId": 4, "roomName": "Deluxe Otaq", "adults": 1, "children": 1, "pricePerNight": 460, "total": 1085.60 }
  ],
  "totalPrice": 1982.40,
  "paymentId": 7
}
```

Təkrar otaq ID-si, otaq sayının uyğunsuzluğu, kifayət etməyən tutum, başqa otelin otağı, qiymət fərqi və ya tarix toqquşması `400` qaytarır. Bütün otaqlar birlikdə qəbul edilir. Əvvəlki klientlər bir otaq üçün `roomId` və `guests` göndərə bilər. Köhnə qeydlərdə böyük/uşaq bölgüsü saxlanmadığından oxunuşda həmin qonaq sayı böyüklər sahəsində görünür.

| Metod və yol | İstifadə |
| --- | --- |
| `GET /api/bookings/mine` | İstifadəçinin rezervasiyaları və otaqları. |
| `GET /api/bookings` | Mövcud rol və otel icazələrinə uyğun idarəetmə siyahısı. |
| `GET /api/bookings/{id}` | Rezervasiya və otaqlar üzrə detallar. |
| `PUT /api/bookings/{id}` | Mövcud müqavilə ilə tarix/qonaq sayını dəyişir və bütün otaqları yoxlayır. |
| `GET /api/bookings/{id}/receipt` | Otaq adları daxil edilmiş HTML qəbz. |
| `POST /api/bookings/{id}/email-receipt` | Mövcud email servisi ilə HTML təsdiqi və PDF əlavə. |

## Migration və tətbiq

`20261106175716_AddBookingRooms` yeni otaq qeydlərini saxlayır. Köhnə rezervasiyalar `Bookings.RoomId` ilə oxunur. Migration yalnız ayrıca test bazasında tətbiq edilib. API start zamanı gözləyən migration-ları avtomatik tətbiq etdiyi üçün bu versiyanın işlək bazaya qoşulması ayrıca təsdiqdən sonra edilməlidir.
