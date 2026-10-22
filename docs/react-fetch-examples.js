const API_BASE_URL = "http://localhost:5080";

async function requestJson(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      Accept: "application/json",
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...(options.headers ?? {}),
    },
    ...options,
  });

  if (response.status === 204) {
    return null;
  }

  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw new Error(data.message || `Request failed with status ${response.status}`);
  }

  return data;
}

export function searchHotels({ city, checkIn, checkOut, guests } = {}) {
  const query = new URLSearchParams();
  if (city) query.set("city", city);
  if (checkIn) query.set("checkIn", checkIn);
  if (checkOut) query.set("checkOut", checkOut);
  if (guests) query.set("guests", String(guests));
  const suffix = query.toString() ? `?${query}` : "";
  return requestJson(`/api/hotels${suffix}`);
}

export function searchFlights({ origin, destination, date, passengers, sort = "price" } = {}) {
  const query = new URLSearchParams();
  if (origin) query.set("origin", origin);
  if (destination) query.set("destination", destination);
  if (date) query.set("date", date);
  if (passengers) query.set("passengers", String(passengers));
  if (sort) query.set("sort", sort);
  return requestJson(`/api/flights?${query}`);
}

export function searchTours({ destination, date, guests } = {}) {
  const query = new URLSearchParams();
  if (destination) query.set("destination", destination);
  if (date) query.set("date", date);
  if (guests) query.set("guests", String(guests));
  const suffix = query.toString() ? `?${query}` : "";
  return requestJson(`/api/tours${suffix}`);
}

export function getTripSuggestions({ city, checkIn, checkOut, guests } = {}) {
  const query = new URLSearchParams();
  if (city) query.set("city", city);
  if (checkIn) query.set("checkIn", checkIn);
  if (checkOut) query.set("checkOut", checkOut);
  if (guests) query.set("guests", String(guests));
  return requestJson(`/api/trip-suggestions?${query}`);
}

export function getMyReservations(token, bookingType) {
  const suffix = bookingType ? `?bookingType=${encodeURIComponent(bookingType)}` : "";
  return requestJson(`/api/bookings/mine${suffix}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export function createBooking(token, booking) {
  return requestJson("/api/bookings", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: JSON.stringify(booking),
  });
}

/*
Hotel axtarisi:

const hotels = await searchHotels({
  city: "Antalya",
  checkIn: "2026-11-10",
  checkOut: "2026-11-14",
  guests: 2,
});

Otel rezervasiyasi (eyni hesab, sonra optional ucus/tur teklifi):

const booking = await createBooking(token, {
  bookingType: "hotel",
  hotelId: 3,
  roomId: 5,
  guestName: "Aytac Ahmedova",
  guestEmail: "guest@example.com",
  checkIn: "2026-11-10",
  checkOut: "2026-11-14",
  guests: 2,
  paymentMethod: "card",
});

booking.tripSuggestions; // mecburi deyil

Ucus:

await createBooking(token, {
  bookingType: "flight",
  flightId: 1,
  guestName: "Aytac Ahmedova",
  guestEmail: "guest@example.com",
  guests: 2,
  paymentMethod: "card",
});

Tur:

await createBooking(token, {
  bookingType: "tour",
  tourId: 1,
  travelDate: "2026-11-15",
  guestName: "Aytac Ahmedova",
  guestEmail: "guest@example.com",
  guests: 2,
  paymentMethod: "card",
});

Rezervasiyalarim:

const mine = await getMyReservations(token);
*/
