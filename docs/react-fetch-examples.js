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

export function getHotels() {
  return requestJson("/api/hotels");
}

export function getHotel(id) {
  return requestJson(`/api/hotels/${id}`);
}

export function createHotel(hotel) {
  return requestJson("/api/hotels", {
    method: "POST",
    body: JSON.stringify(hotel),
  });
}

export function updateHotel(id, hotel) {
  return requestJson(`/api/hotels/${id}`, {
    method: "PUT",
    body: JSON.stringify(hotel),
  });
}

export function deleteHotel(id) {
  return requestJson(`/api/hotels/${id}`, {
    method: "DELETE",
  });
}

export function getRooms() {
  return requestJson("/api/rooms");
}

export function getHotelRooms(hotelId) {
  return requestJson(`/api/hotels/${hotelId}/rooms`);
}

export function getRoom(id) {
  return requestJson(`/api/rooms/${id}`);
}

export function createRoom(room) {
  return requestJson("/api/rooms", {
    method: "POST",
    body: JSON.stringify(room),
  });
}

export function updateRoom(id, room) {
  return requestJson(`/api/rooms/${id}`, {
    method: "PUT",
    body: JSON.stringify(room),
  });
}

export function deleteRoom(id) {
  return requestJson(`/api/rooms/${id}`, {
    method: "DELETE",
  });
}

export function getReservations() {
  return requestJson("/api/reservations");
}

export function getReservation(id) {
  return requestJson(`/api/reservations/${id}`);
}

export function createReservation(reservation) {
  return requestJson("/api/reservations", {
    method: "POST",
    body: JSON.stringify(reservation),
  });
}

export function updateReservation(id, reservation) {
  return requestJson(`/api/reservations/${id}`, {
    method: "PUT",
    body: JSON.stringify(reservation),
  });
}

export function deleteReservation(id) {
  return requestJson(`/api/reservations/${id}`, {
    method: "DELETE",
  });
}

/*
React component icinde ornek:

import { getHotels, createReservation } from "./api/aeHotelApi";

const hotels = await getHotels();

const reservation = await createReservation({
  hotelId: 1,
  roomId: 1,
  guestName: "Aytac Ahmedova",
  guestEmail: "guest@example.com",
  checkIn: "2026-11-10",
  checkOut: "2026-11-14",
  guests: 2,
  status: "confirmed",
});
*/
