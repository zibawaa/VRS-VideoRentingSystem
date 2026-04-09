import { useEffect, useState, useCallback } from 'react';
import type { FormEvent } from 'react';
import './Admin.css';

/* ─── domain types for admin API responses ─── */

type AdminTab = 'users' | 'videos' | 'rentals';

type SessionInfo = { token: string; expiresAtUtc: string };

type AdminUser = {
  userId: number;
  username: string;
  role: string;
  studioName?: string | null;
  sessions: SessionInfo[];
};

type AdminVideo = {
  videoId: number;
  title: string;
  genre: string;
  releaseYear: number;
  isRented: boolean;
  ownerPublisherId: number;
  type: string;
  rentalPrice: number;
  rentalHours: number;
  isPublished: boolean;
};

type AdminRental = {
  userId: number;
  username: string;
  videoId: number;
  title: string;
  genre: string;
  rentDateUtc: string;
  expiryUtc: string;
  paidAmount: number;
};

/* ─── constants ─── */

const API = 'http://localhost:5265';
const ADMIN_KEY = 'vrs-admin-auth';

/* ─── fetch helper ─── */

// generic fetch wrapper that attaches the admin bearer token and parses JSON
async function adminApi<T>(path: string, opts: RequestInit = {}, token?: string): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(opts.headers as Record<string, string> | undefined),
  };
  // attach bearer token when the caller has an active admin session
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`${API}${path}`, { ...opts, headers });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(body || `HTTP ${res.status}`);
  }
  // 204 No Content has no body to parse
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

/* ═══════════════════════════════════════════════════════
   Admin — full system management panel
   ═══════════════════════════════════════════════════════ */

export default function Admin() {
  // identity and navigation state for the admin session
  const [token, setToken] = useState('');
  const [adminName, setAdminName] = useState('');
  const [tab, setTab] = useState<AdminTab>('users');
  const [status, setStatus] = useState('');

  /* ── login form ── */
  const [loginUser, setLoginUser] = useState('');
  const [loginPass, setLoginPass] = useState('');

  /* ── data lists ── */
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [videos, setVideos] = useState<AdminVideo[]>([]);
  const [rentals, setRentals] = useState<AdminRental[]>([]);

  /* ── expanded user rows to show tokens ── */
  const [expandedUsers, setExpandedUsers] = useState<Set<number>>(new Set());

  /* ── video edit state ── */
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editForm, setEditForm] = useState<Partial<AdminVideo>>({});

  /* ── create video form ── */
  const [newId, setNewId] = useState('');
  const [newTitle, setNewTitle] = useState('');
  const [newGenre, setNewGenre] = useState('');
  const [newYear, setNewYear] = useState('');
  const [newType, setNewType] = useState('Movie');
  const [newPrice, setNewPrice] = useState('');
  const [newHours, setNewHours] = useState('');
  const [newOwner, setNewOwner] = useState('');
  const [newPublished, setNewPublished] = useState(true);

  /* ── rent-on-behalf form ── */
  const [rentUserId, setRentUserId] = useState('');
  const [rentVideoId, setRentVideoId] = useState('');

  /* ── restore session on mount ── */
  useEffect(() => {
    const raw = localStorage.getItem(ADMIN_KEY);
    if (!raw) return;
    // try to parse the persisted admin session from localStorage
    try {
      const data = JSON.parse(raw);
      setToken(data.token);
      setAdminName(data.username);
    } catch {
      // corrupt storage entry — clear it so login screen shows
      localStorage.removeItem(ADMIN_KEY);
    }
  }, []);

  /* ── data refresh ── */
  // fetches the data list for whichever tab is currently active
  const refresh = useCallback(async () => {
    if (!token) return;
    try {
      if (tab === 'users') {
        setUsers(await adminApi<AdminUser[]>('/api/admin/users', {}, token));
      } else if (tab === 'videos') {
        setVideos(await adminApi<AdminVideo[]>('/api/admin/videos', {}, token));
      } else if (tab === 'rentals') {
        setRentals(await adminApi<AdminRental[]>('/api/admin/rentals', {}, token));
      }
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Request failed.');
    }
  }, [token, tab]);

  // re-fetch whenever the active tab or token changes
  useEffect(() => { void refresh(); }, [refresh]);

  /* ── admin login ── */
  async function handleLogin(e: FormEvent) {
    e.preventDefault();
    try {
      // authenticate against the admin-only endpoint which rejects non-admin roles
      const res = await adminApi<{ token: string; username: string }>('/api/admin/login', {
        method: 'POST',
        body: JSON.stringify({ username: loginUser, password: loginPass }),
      });
      // store session in both React state and localStorage for persistence
      setToken(res.token);
      setAdminName(res.username);
      localStorage.setItem(ADMIN_KEY, JSON.stringify(res));
      setStatus('Logged in as admin.');
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Login failed.');
    }
  }

  // clears the admin session from state and storage
  function handleLogout() {
    setToken('');
    setAdminName('');
    localStorage.removeItem(ADMIN_KEY);
    setStatus('Signed out.');
  }

  /* ── user row expand toggle ── */
  // toggles the expanded state for a user row to show/hide their active tokens
  function toggleExpand(userId: number) {
    setExpandedUsers((prev) => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  }

  /* ── video actions ── */

  // populates the inline edit form with the current video values
  function startEdit(v: AdminVideo) {
    setEditingId(v.videoId);
    setEditForm({ ...v });
  }

  function cancelEdit() {
    setEditingId(null);
    setEditForm({});
  }

  // sends the edited fields to the PUT endpoint and refreshes the table
  async function saveEdit() {
    if (editingId === null) return;
    try {
      await adminApi(`/api/admin/videos/${editingId}`, {
        method: 'PUT',
        body: JSON.stringify(editForm),
      }, token);
      setStatus(`Video #${editingId} updated.`);
      cancelEdit();
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Update failed.');
    }
  }

  // flips the published/draft flag via the PATCH toggle endpoint
  async function togglePublish(videoId: number) {
    try {
      await adminApi(`/api/admin/videos/${videoId}/publish`, { method: 'PATCH' }, token);
      setStatus(`Toggled publish for #${videoId}.`);
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Toggle failed.');
    }
  }

  // permanently removes a video from the catalog
  async function deleteVideo(videoId: number) {
    try {
      await adminApi(`/api/admin/videos/${videoId}`, { method: 'DELETE' }, token);
      setStatus(`Deleted video #${videoId}.`);
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Delete failed.');
    }
  }

  // parses numeric fields from the text inputs and posts a new video
  async function handleCreateVideo(e: FormEvent) {
    e.preventDefault();
    try {
      await adminApi('/api/admin/videos', {
        method: 'POST',
        body: JSON.stringify({
          videoId: parseInt(newId, 10),
          title: newTitle,
          genre: newGenre,
          releaseYear: parseInt(newYear, 10),
          type: newType,
          rentalPrice: parseFloat(newPrice),
          rentalHours: parseInt(newHours, 10),
          isPublished: newPublished,
          ownerPublisherId: newOwner.trim() ? parseInt(newOwner, 10) : undefined,
        }),
      }, token);
      setStatus(`Video created.`);
      // reset form fields after successful creation
      setNewId(''); setNewTitle(''); setNewGenre(''); setNewYear('');
      setNewPrice(''); setNewHours(''); setNewOwner('');
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Create failed.');
    }
  }

  /* ── rental actions ── */

  // forces a return on behalf of any user via the admin endpoint
  async function adminReturn(videoId: number, userId: number) {
    try {
      await adminApi(`/api/admin/rentals/${videoId}/return`, {
        method: 'POST',
        body: JSON.stringify({ userId }),
      }, token);
      setStatus(`Returned video #${videoId} for user #${userId}.`);
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Return failed.');
    }
  }

  // rents a video on behalf of a target user specified in the form
  async function handleAdminRent(e: FormEvent) {
    e.preventDefault();
    // parse the two numeric IDs from the text inputs
    const vid = parseInt(rentVideoId, 10);
    const uid = parseInt(rentUserId, 10);
    if (isNaN(vid) || isNaN(uid)) {
      setStatus('Enter valid user ID and video ID.');
      return;
    }
    try {
      await adminApi(`/api/admin/rentals/${vid}/rent`, {
        method: 'POST',
        body: JSON.stringify({ userId: uid }),
      }, token);
      setStatus(`Rented video #${vid} for user #${uid}.`);
      // clear the form after a successful rent
      setRentVideoId('');
      setRentUserId('');
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Rent failed.');
    }
  }

  /* ══════════ RENDER ══════════ */

  // when there is no token the user hasn't authenticated yet — show the login card
  if (!token) {
    return (
      <div className="admin-app">
        <div className="admin-login-wrapper">
          <div className="admin-login-card">
            <div className="admin-login-icon">
              <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
              </svg>
            </div>
            <h1 className="admin-login-title">Admin Panel</h1>
            <p className="admin-login-subtitle">Sign in with an admin account</p>
            <form onSubmit={handleLogin} className="admin-login-form">
              <input
                className="admin-input"
                value={loginUser}
                onChange={(e) => setLoginUser(e.target.value)}
                placeholder="Username"
                required
              />
              <input
                className="admin-input"
                type="password"
                value={loginPass}
                onChange={(e) => setLoginPass(e.target.value)}
                placeholder="Password"
                required
              />
              <button className="admin-btn admin-btn-primary" type="submit">Sign in</button>
            </form>
            {status && <div className="admin-login-status">{status}</div>}
            <a href="#/" className="admin-back-link">Back to marketplace</a>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-app">
      {/* ── header ── */}
      <header className="admin-header">
        <div className="admin-header-left">
          <span className="admin-logo">VRS</span>
          <span className="admin-badge">ADMIN</span>
        </div>
        <div className="admin-header-right">
          <span className="admin-user">{adminName}</span>
          <div className="admin-token-display">
            <span className="admin-token-label">Token:</span>
            <code className="admin-token-value">{token}</code>
          </div>
          <button className="admin-btn admin-btn-ghost" onClick={handleLogout}>Sign out</button>
          <a href="#/" className="admin-btn admin-btn-ghost">Marketplace</a>
        </div>
      </header>

      {/* ── tab bar ── */}
      <nav className="admin-tabs">
        {(['users', 'videos', 'rentals'] as AdminTab[]).map((t) => (
          <button
            key={t}
            className={`admin-tab ${tab === t ? 'active' : ''}`}
            onClick={() => setTab(t)}
          >
            {t === 'users' ? 'Users' : t === 'videos' ? 'Videos' : 'Rentals'}
          </button>
        ))}
      </nav>

      <main className="admin-main">
        {/* ══════════ USERS TAB ══════════ */}
        {tab === 'users' && (
          <section className="admin-section">
            <div className="admin-section-header">
              <h2>All Users</h2>
              <span className="admin-count">{users.length}</span>
            </div>
            <div className="admin-table-wrapper">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Username</th>
                    <th>Role</th>
                    <th>Studio</th>
                    <th>Sessions</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <>
                      <tr key={u.userId}>
                        <td className="mono">{u.userId}</td>
                        <td><strong>{u.username}</strong></td>
                        <td><span className={`admin-role-badge ${u.role.toLowerCase()}`}>{u.role}</span></td>
                        <td>{u.studioName || '—'}</td>
                        <td>{u.sessions.length} active</td>
                        <td>
                          <button
                            className="admin-btn admin-btn-sm admin-btn-outline"
                            onClick={() => toggleExpand(u.userId)}
                          >
                            {expandedUsers.has(u.userId) ? 'Hide tokens' : 'Show tokens'}
                          </button>
                        </td>
                      </tr>
                      {/* collapsible row that reveals active session tokens and expiry timestamps */}
                      {expandedUsers.has(u.userId) && (
                        <tr key={`${u.userId}-tokens`} className="admin-token-row">
                          <td colSpan={6}>
                            {u.sessions.length === 0 ? (
                              <span className="admin-muted">No active sessions</span>
                            ) : (
                              <div className="admin-token-list">
                                {u.sessions.map((s, i) => (
                                  <div key={i} className="admin-token-item">
                                    <code className="admin-token-code">{s.token}</code>
                                    <span className="admin-token-expiry">
                                      expires {new Date(s.expiresAtUtc).toLocaleString()}
                                    </span>
                                  </div>
                                ))}
                              </div>
                            )}
                          </td>
                        </tr>
                      )}
                    </>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}

        {/* ══════════ VIDEOS TAB ══════════ */}
        {tab === 'videos' && (
          <section className="admin-section">
            <div className="admin-section-header">
              <h2>All Videos</h2>
              <span className="admin-count">{videos.length}</span>
            </div>

            {/* create form */}
            <details className="admin-details">
              <summary className="admin-summary">Create new video</summary>
              <form className="admin-create-form" onSubmit={handleCreateVideo}>
                <input className="admin-input" value={newId} onChange={(e) => setNewId(e.target.value)} placeholder="Video ID" required />
                <input className="admin-input" value={newTitle} onChange={(e) => setNewTitle(e.target.value)} placeholder="Title" required />
                <input className="admin-input" value={newGenre} onChange={(e) => setNewGenre(e.target.value)} placeholder="Genre" required />
                <input className="admin-input" value={newYear} onChange={(e) => setNewYear(e.target.value)} placeholder="Year" required />
                <select className="admin-input" value={newType} onChange={(e) => setNewType(e.target.value)}>
                  <option value="Movie">Movie</option>
                  <option value="Series">Series</option>
                </select>
                <input className="admin-input" value={newPrice} onChange={(e) => setNewPrice(e.target.value)} placeholder="Price" required />
                <input className="admin-input" value={newHours} onChange={(e) => setNewHours(e.target.value)} placeholder="Rental hours" required />
                <input className="admin-input" value={newOwner} onChange={(e) => setNewOwner(e.target.value)} placeholder="Owner publisher ID" />
                <label className="admin-checkbox">
                  <input type="checkbox" checked={newPublished} onChange={(e) => setNewPublished(e.target.checked)} />
                  Published
                </label>
                <button className="admin-btn admin-btn-primary" type="submit">Create</button>
              </form>
            </details>

            <div className="admin-table-wrapper">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Title</th>
                    <th>Genre</th>
                    <th>Year</th>
                    <th>Type</th>
                    <th>Price</th>
                    <th>Hours</th>
                    <th>Owner</th>
                    <th>Published</th>
                    <th>Rented</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {/* each video row flips between read-only display and inline edit mode */}
                  {videos.map((v) =>
                    editingId === v.videoId ? (
                      <tr key={v.videoId} className="admin-edit-row">
                        <td className="mono">{v.videoId}</td>
                        <td><input className="admin-input-sm" value={editForm.title ?? ''} onChange={(e) => setEditForm({ ...editForm, title: e.target.value })} /></td>
                        <td><input className="admin-input-sm" value={editForm.genre ?? ''} onChange={(e) => setEditForm({ ...editForm, genre: e.target.value })} /></td>
                        <td><input className="admin-input-sm" type="number" value={editForm.releaseYear ?? ''} onChange={(e) => setEditForm({ ...editForm, releaseYear: parseInt(e.target.value, 10) })} /></td>
                        <td>
                          <select className="admin-input-sm" value={editForm.type ?? 'Movie'} onChange={(e) => setEditForm({ ...editForm, type: e.target.value })}>
                            <option value="Movie">Movie</option>
                            <option value="Series">Series</option>
                          </select>
                        </td>
                        <td><input className="admin-input-sm" type="number" step="0.01" value={editForm.rentalPrice ?? ''} onChange={(e) => setEditForm({ ...editForm, rentalPrice: parseFloat(e.target.value) })} /></td>
                        <td><input className="admin-input-sm" type="number" value={editForm.rentalHours ?? ''} onChange={(e) => setEditForm({ ...editForm, rentalHours: parseInt(e.target.value, 10) })} /></td>
                        <td><input className="admin-input-sm" type="number" value={editForm.ownerPublisherId ?? ''} onChange={(e) => setEditForm({ ...editForm, ownerPublisherId: parseInt(e.target.value, 10) })} /></td>
                        <td>{editForm.isPublished ? 'Yes' : 'No'}</td>
                        <td>{v.isRented ? 'Yes' : 'No'}</td>
                        <td className="admin-actions">
                          <button className="admin-btn admin-btn-sm admin-btn-primary" onClick={saveEdit}>Save</button>
                          <button className="admin-btn admin-btn-sm admin-btn-ghost" onClick={cancelEdit}>Cancel</button>
                        </td>
                      </tr>
                    ) : (
                      <tr key={v.videoId}>
                        <td className="mono">{v.videoId}</td>
                        <td><strong>{v.title}</strong></td>
                        <td>{v.genre}</td>
                        <td>{v.releaseYear}</td>
                        <td>{v.type}</td>
                        <td>${v.rentalPrice.toFixed(2)}</td>
                        <td>{v.rentalHours}h</td>
                        <td className="mono">{v.ownerPublisherId}</td>
                        <td>
                          <button
                            className={`admin-badge-toggle ${v.isPublished ? 'published' : 'draft'}`}
                            onClick={() => togglePublish(v.videoId)}
                          >
                            {v.isPublished ? 'Published' : 'Draft'}
                          </button>
                        </td>
                        <td>
                          <span className={`admin-status ${v.isRented ? 'rented' : 'available'}`}>
                            {v.isRented ? 'Rented' : 'Available'}
                          </span>
                        </td>
                        <td className="admin-actions">
                          <button className="admin-btn admin-btn-sm admin-btn-outline" onClick={() => startEdit(v)}>Edit</button>
                          <button className="admin-btn admin-btn-sm admin-btn-danger" onClick={() => deleteVideo(v.videoId)}>Delete</button>
                        </td>
                      </tr>
                    )
                  )}
                </tbody>
              </table>
            </div>
          </section>
        )}

        {/* ══════════ RENTALS TAB ══════════ */}
        {tab === 'rentals' && (
          <section className="admin-section">
            <div className="admin-section-header">
              <h2>All Rentals</h2>
              <span className="admin-count">{rentals.length}</span>
            </div>

            {/* rent-on-behalf form */}
            <details className="admin-details">
              <summary className="admin-summary">Rent on behalf of a user</summary>
              <form className="admin-create-form admin-rent-form" onSubmit={handleAdminRent}>
                <input className="admin-input" value={rentUserId} onChange={(e) => setRentUserId(e.target.value)} placeholder="User ID" required />
                <input className="admin-input" value={rentVideoId} onChange={(e) => setRentVideoId(e.target.value)} placeholder="Video ID" required />
                <button className="admin-btn admin-btn-primary" type="submit">Rent</button>
              </form>
            </details>

            <div className="admin-table-wrapper">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>User ID</th>
                    <th>Username</th>
                    <th>Video ID</th>
                    <th>Title</th>
                    <th>Genre</th>
                    <th>Rented</th>
                    <th>Expires</th>
                    <th>Paid</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {rentals.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="admin-empty">No active rentals.</td>
                    </tr>
                  ) : (
                    rentals.map((r) => (
                      <tr key={`${r.userId}-${r.videoId}`}>
                        <td className="mono">{r.userId}</td>
                        <td><strong>{r.username}</strong></td>
                        <td className="mono">{r.videoId}</td>
                        <td>{r.title}</td>
                        <td>{r.genre}</td>
                        <td>{new Date(r.rentDateUtc).toLocaleString()}</td>
                        <td>{new Date(r.expiryUtc).toLocaleString()}</td>
                        <td>${r.paidAmount.toFixed(2)}</td>
                        <td>
                          <button
                            className="admin-btn admin-btn-sm admin-btn-outline"
                            onClick={() => adminReturn(r.videoId, r.userId)}
                          >
                            Force return
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        )}
      </main>

      {/* ── status bar ── */}
      <div className="admin-status-bar">
        <span className="admin-status-dot" />
        {status || 'Ready.'}
      </div>
    </div>
  );
}
