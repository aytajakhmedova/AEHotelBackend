import React, { Fragment, useEffect, useMemo, useState } from 'react';
import { FaRegTrashAlt } from 'react-icons/fa';
import { createStaff, deleteStaff, getHotels, getStaff, updateStaff } from '../../../api/client';
import { canManageUser, getAuthUser, hasRole } from '../../../api/auth';
import '../Hotels/AdminHotels.css';
import '../Manage/AdminManage.css';

const ROLE_OPTIONS = [
  { value: 'SuperAdmin', label: 'Super Admin' },
  { value: 'Admin', label: 'Admin' },
  { value: 'HotelManager', label: 'Hotel Manager' },
  { value: 'Receptionist', label: 'Receptionist' },
];
const roleLabel = (role) => {
  if (role === 'SuperAdmin') return 'Super Admin';
  return ROLE_OPTIONS.find((item) => item.value === role)?.label || role;
};
const needsHotel = (role) => role === 'HotelManager' || role === 'Receptionist';

const EMPTY = {
  id: '',
  name: '',
  surname: '',
  email: '',
  phone: '',
  jobTitle: '',
  role: 'Receptionist',
  assignedHotelId: '',
  isActive: true,
  password: '',
};

const AdminStaff = () => {
  const [staff, setStaff] = useState([]);
  const [hotels, setHotels] = useState([]);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [editing, setEditing] = useState(null);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  const [message, setMessage] = useState('');
  const currentEmail = (getAuthUser()?.email || '').toLowerCase();
  const roleOptions = ROLE_OPTIONS.filter((role) => role.value !== 'SuperAdmin' || hasRole('SuperAdmin'));

  const load = () => {
    setLoading(true);
    return Promise.all([getStaff(), getHotels()])
      .then(([items, hotelItems]) => {
        setStaff(Array.isArray(items) ? items : []);
        setHotels(Array.isArray(hotelItems) ? hotelItems : []);
        setLoadError('');
      })
      .catch((error) => {
        setStaff([]);
        setHotels([]);
        setLoadError(error.message || 'İşçilər yüklənmədi.');
      })
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, []);

  const visible = useMemo(() => {
    const query = search.trim().toLowerCase();
    return staff.filter((member) => {
      if (roleFilter !== 'all' && member.role !== roleFilter) return false;
      if (statusFilter === 'active' && !member.isActive) return false;
      if (statusFilter === 'passive' && member.isActive) return false;
      if (!query) return true;
      return [member.name, member.surname, member.email, member.phone, member.jobTitle, member.assignedHotelName]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(query);
    });
  }, [staff, search, roleFilter, statusFilter]);

  const startNew = () => {
    if (!hasRole('SuperAdmin', 'Admin')) return;
    setMessage('');
    setFormError('');
    setEditing({ ...EMPTY, assignedHotelId: hotels[0]?.id || '' });
  };

  const startEdit = (member) => {
    if (!canManageUser(member)) return;
    setMessage('');
    setFormError('');
    setEditing({ ...member, assignedHotelId: member.assignedHotelId || '', password: '' });
  };

  const change = (field, value) => setEditing((prev) => ({ ...prev, [field]: value }));

  const save = (event) => {
    event.preventDefault();
    const target = editing?.id ? staff.find((member) => member.id === editing.id) : editing;
    if (!canManageUser(target) || (editing?.role === 'SuperAdmin' && !hasRole('SuperAdmin'))) {
      setFormError('Bu əməliyyat üçün icazəniz yoxdur.');
      return;
    }
    setSaving(true);
    setFormError('');
    const payload = {
      name: editing.name,
      surname: editing.surname,
      email: editing.email,
      phone: editing.phone,
      jobTitle: editing.jobTitle,
      role: editing.role,
      assignedHotelId: needsHotel(editing.role) ? Number(editing.assignedHotelId) || null : null,
      isActive: editing.isActive,
      password: editing.password || null,
    };
    const request = editing.id ? updateStaff(editing.id, payload) : createStaff(payload);
    request
      .then((saved) => {
        setStaff((prev) => (editing.id ? prev.map((item) => (item.id === saved.id ? saved : item)) : [saved, ...prev]));
        setMessage(editing.id ? `${saved.email} yeniləndi.` : `${saved.email} əlavə edildi.`);
        setEditing(null);
      })
      .catch((error) => setFormError(error.message || 'Yadda saxlanmadı.'))
      .finally(() => setSaving(false));
  };

  const remove = (member) => {
    if (!canManageUser(member)) return;
    if (!window.confirm(`${member.name} ${member.surname} işçi siyahısından silinsin?`)) return;
    setMessage('');
    deleteStaff(member.id)
      .then(() => {
        setStaff((prev) => prev.filter((item) => item.id !== member.id));
        setMessage(`${member.email} silindi.`);
      })
      .catch((error) => setMessage(error.message || 'Silinmədi.'));
  };

  const form = (
    <tr className="edit-row">
      <td colSpan="7">
        <form className="admin-edit-card" onSubmit={save}>
          <h3>{editing?.id ? 'İşçini redaktə et' : 'Yeni işçi'}</h3>
          <div className="admin-form-grid">
            <label>
              Ad
              <input value={editing?.name || ''} onChange={(e) => change('name', e.target.value)} required />
            </label>
            <label>
              Soyad
              <input value={editing?.surname || ''} onChange={(e) => change('surname', e.target.value)} />
            </label>
            <label>
              Email
              <input type="email" value={editing?.email || ''} onChange={(e) => change('email', e.target.value)} required />
            </label>
            <label>
              Telefon
              <input value={editing?.phone || ''} onChange={(e) => change('phone', e.target.value)} />
            </label>
            <label>
              Vəzifə
              <input value={editing?.jobTitle || ''} onChange={(e) => change('jobTitle', e.target.value)} placeholder="Məsələn: Ön qəbul rəhbəri" />
            </label>
            <label>
              Rol
              <select value={editing?.role} onChange={(e) => change('role', e.target.value)}>
                {roleOptions.map((role) => (
                  <option key={role.value} value={role.value}>{role.label}</option>
                ))}
              </select>
            </label>
            {needsHotel(editing?.role) && (
              <label>
                Otel
                <select value={editing?.assignedHotelId || ''} onChange={(e) => change('assignedHotelId', e.target.value)} required>
                  <option value="" disabled>Otel seçin</option>
                  {hotels.map((hotel) => (
                    <option key={hotel.id} value={hotel.id}>{hotel.title}</option>
                  ))}
                </select>
              </label>
            )}
            <label>
              {editing?.id ? 'Yeni parol (boş qalsa dəyişmir)' : 'Parol'}
              <input
                type="password"
                value={editing?.password || ''}
                onChange={(e) => change('password', e.target.value)}
                required={!editing?.id}
                autoComplete="new-password"
              />
            </label>
            <label className="checkbox-row">
              <input type="checkbox" checked={!!editing?.isActive} onChange={(e) => change('isActive', e.target.checked)} />
              Aktiv (passiv işçi sistemə daxil ola bilmir)
            </label>
          </div>
          {formError && <p className="manage-message error">{formError}</p>}
          <div className="admin-form-actions">
            <button type="submit" className="btn-admin-primary" disabled={saving}>
              {saving ? 'Yadda saxlanır...' : 'Yadda saxla'}
            </button>
            <button type="button" onClick={() => setEditing(null)}>Ləğv et</button>
          </div>
        </form>
      </td>
    </tr>
  );

  return (
    <div className="admin-page">
      <div className="page-header-row">
        <div className="page-header">
          <h1 className="page-title">İşçilər</h1>
          <p className="page-subtitle">Admin, Hotel Manager və Receptionist hesabları</p>
        </div>
        {!loading && !loadError && hasRole('SuperAdmin', 'Admin') && (
          <button type="button" className="btn-admin-primary" onClick={startNew}>İşçi əlavə et</button>
        )}
      </div>

      {loading && <p className="page-subtitle">Yüklənir...</p>}
      {loadError && (
        <div className="coming-soon-card">
          <h2>Məlumat yüklənmədi</h2>
          <p>{loadError}</p>
          <div className="admin-form-actions">
            <button type="button" className="btn-admin-primary" onClick={load}>Yenidən yoxla</button>
          </div>
        </div>
      )}

      {!loading && !loadError && (
        <>
          <div className="manage-toolbar">
            <input
              className="grow"
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Ad, email, vəzifə və ya otel axtar"
            />
            <select value={roleFilter} onChange={(e) => setRoleFilter(e.target.value)}>
              <option value="all">Bütün rollar</option>
              {ROLE_OPTIONS.map((role) => (
                <option key={role.value} value={role.value}>{role.label}</option>
              ))}
            </select>
            <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="all">Hər status</option>
              <option value="active">Aktiv</option>
              <option value="passive">Passiv</option>
            </select>
          </div>
          {message && <p className="manage-message">{message}</p>}

          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Ad Soyad</th>
                  <th>Email / Telefon</th>
                  <th>Vəzifə</th>
                  <th>Rol</th>
                  <th>Otel</th>
                  <th>Status</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {editing && !editing.id && canManageUser(editing) && form}
                {visible.map((member) => (
                  <Fragment key={member.id}>
                    {editing?.id === member.id && canManageUser(member) ? (
                      form
                    ) : (
                      <tr>
                        <td><strong>{member.name} {member.surname !== member.name ? member.surname : ''}</strong></td>
                        <td>
                          {member.email}
                          <br />
                          <small>{member.phone || '—'}</small>
                        </td>
                        <td>{member.jobTitle || '—'}</td>
                        <td>{roleLabel(member.role)}</td>
                        <td>{member.assignedHotelName || '—'}</td>
                        <td>
                          <span className={`status-badge ${member.isActive ? 'status-active' : 'status-passive'}`}>
                            {member.isActive ? 'Aktiv' : 'Passiv'}
                          </span>
                        </td>
                        <td className="table-actions">
                          {canManageUser(member) && <button type="button" onClick={() => startEdit(member)}>Redaktə</button>}
                          {canManageUser(member) && member.email.toLowerCase() !== currentEmail && (
                            <button type="button" className="danger" onClick={() => remove(member)} aria-label="Sil" title="Sil">
                              <FaRegTrashAlt />
                            </button>
                          )}
                        </td>
                      </tr>
                    )}
                  </Fragment>
                ))}
                {visible.length === 0 && !editing && (
                  <tr>
                    <td colSpan="7">{staff.length === 0 ? 'İşçi yoxdur' : 'Uyğun işçi tapılmadı'}</td>
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

export default AdminStaff;
