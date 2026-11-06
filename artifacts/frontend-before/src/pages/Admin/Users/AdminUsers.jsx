import React, { useEffect, useMemo, useState } from 'react';
import { getHotels, getUsers, updateUserRole } from '../../../api/client';
import '../Hotels/AdminHotels.css';

// Role options - SuperAdmin cannot be assigned via UI (backend only)
const ROLE_OPTIONS = [
  { value: 'Admin', label: 'Admin' },
  { value: 'HotelManager', label: 'Hotel Manager' },
  { value: 'Receptionist', label: 'Receptionist' },
  { value: 'Customer', label: 'Customer' },
  { value: 'Member', label: 'Member' },
];

const primaryRole = (roles) => (Array.isArray(roles) && roles[0]) || 'Customer';

const needsHotel = (role) => role === 'HotelManager' || role === 'Receptionist';

const AdminUsers = () => {
  const [users, setUsers] = useState([]);
  const [hotels, setHotels] = useState([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [savingId, setSavingId] = useState('');
  const [message, setMessage] = useState('');

  const loadUsers = (silent = false) => {
    if (!silent) setLoading(true);
    return Promise.all([getUsers(), getHotels()])
      .then(([items, hotelItems]) => {
        setUsers(Array.isArray(items) ? items : []);
        setHotels(Array.isArray(hotelItems) ? hotelItems : []);
        setLoadError('');
      })
      .catch((error) => {
        setUsers([]);
        setHotels([]);
        setLoadError(error.message || 'İstifadəçilər yüklənmədi.');
      })
      .finally(() => {
        if (!silent) setLoading(false);
      });
  };

  useEffect(() => {
    loadUsers();
  }, []);

  const visible = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return users;
    return users.filter((user) =>
      [user.name, user.email, ...(user.roles || [])]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(query)
    );
  }, [users, search]);

  const saveAssignment = (user, role, assignedHotelId) => {
    const nextRole = role || primaryRole(user.roles);
    const nextHotel = needsHotel(nextRole)
      ? Number(assignedHotelId || user.assignedHotelId || hotels[0]?.id || 0) || null
      : null;
    if (needsHotel(nextRole) && !nextHotel) {
      setMessage('Hotel Manager və Receptionist üçün otel seçilməlidir.');
      return;
    }
    setSavingId(user.id);
    setMessage('');
    updateUserRole(user.id, nextRole, nextHotel)
      .then((updated) => {
        setUsers((prev) => prev.map((item) => (item.id === user.id ? updated : item)));
        setMessage(`${updated.email} üçün rol yeniləndi.`);
      })
      .catch((error) => {
        setMessage(error.message || 'Rol dəyişmədi.');
      })
      .finally(() => setSavingId(''));
  };

  const handleRoleChange = (user, role) => {
    if (!role || (primaryRole(user.roles) === role && !needsHotel(role))) return;
    saveAssignment(user, role, user.assignedHotelId);
  };

  return (
    <div className="admin-page">
      <div className="page-header">
        <h1 className="page-title">İstifadəçilər</h1>
        <p className="page-subtitle">Müştəriləri, işçiləri və onların rollarını idarə edin</p>
      </div>

      {loading && <p className="page-subtitle">Yüklənir...</p>}
      {loadError && (
        <div className="coming-soon-card">
          <h2>Məlumat yüklənmədi</h2>
          <p>{loadError}</p>
          <div className="admin-form-actions">
            <button type="button" className="btn-admin-primary" onClick={() => loadUsers()}>
              Yenidən yoxla
            </button>
          </div>
        </div>
      )}

      {!loading && !loadError && (
        <>
          <div className="inventory-toolbar">
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Ad, email və ya rol axtar"
            />
          </div>
          {message && <p className="page-subtitle">{message}</p>}
          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Ad</th>
                  <th>Email</th>
                  <th>Rol</th>
                  <th>Otel</th>
                  <th>Tarix</th>
                </tr>
              </thead>
              <tbody>
                {visible.map((user) => (
                  <tr key={user.id}>
                    <td><strong>{user.name}</strong></td>
                    <td>{user.email}</td>
                    <td>
                      <select
                        value={primaryRole(user.roles)}
                        disabled={savingId === user.id}
                        onChange={(e) => handleRoleChange(user, e.target.value)}
                      >
                        {ROLE_OPTIONS.map((role) => (
                          <option key={role.value} value={role.value}>{role.label}</option>
                        ))}
                      </select>
                    </td>
                    <td>
                      {needsHotel(primaryRole(user.roles)) ? (
                        <select
                          value={user.assignedHotelId || ''}
                          disabled={savingId === user.id || hotels.length === 0}
                          onChange={(e) => saveAssignment(user, primaryRole(user.roles), e.target.value)}
                        >
                          <option value="" disabled>Otel seçin</option>
                          {hotels.map((hotel) => (
                            <option key={hotel.id} value={hotel.id}>{hotel.title}</option>
                          ))}
                        </select>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td>{user.createdAt ? new Date(user.createdAt).toLocaleDateString('az-AZ') : '—'}</td>
                  </tr>
                ))}
                {visible.length === 0 && (
                  <tr>
                    <td colSpan="5">{users.length === 0 ? 'İstifadəçi yoxdur' : 'Uyğun istifadəçi tapılmadı'}</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
};

export default AdminUsers;
