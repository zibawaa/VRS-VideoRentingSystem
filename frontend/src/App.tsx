import { useEffect, useMemo, useState, useCallback, useRef } from 'react';
import type { FormEvent } from 'react';
import './App.css';

/* ─── domain types mirroring the API response shapes ─── */

type Role = 'Customer' | 'Publisher' | 'Admin';
type Tab = 'browse' | 'rentals' | 'studio';

type AuthUser = {
  token: string;
  userId: number;
  username: string;
  role: Role;
  studioName?: string | null;
};

type Video = {
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

type Rental = {
  videoId: number;
  title: string;
  genre: string;
  rentDateUtc: string;
  expiryUtc: string;
  paidAmount: number;
  isRented: boolean;
};

type AuthResponse = {
  token: string;
  userId: number;
  username: string;
  role: Role;
  studioName?: string | null;
  expiresAtUtc: string;
};

/* ─── agent chat message shape ─── */

type ChatMsg = {
  role: 'user' | 'agent';
  text: string;
};

/* ─── constants ─── */

const API_BASE = 'http://localhost:5265';
const AUTH_KEY = 'vrs-auth';

/* ─── generic fetch wrapper that attaches auth and parses JSON ─── */

async function api<T>(
  path: string,
  opts: RequestInit = {},
  token?: string
): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(opts.headers as Record<string, string> | undefined),
  };

  // attach bearer token when the caller has an active session
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`${API_BASE}${path}`, { ...opts, headers });

  if (!res.ok) {
    const body = await res.text();
    throw new Error(body || `HTTP ${res.status}`);
  }

  // 204 No Content has no body to parse
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

/* ─── helpers ─── */

// first letter of username serves as the avatar initial
function initial(name: string): string {
  return name.charAt(0).toUpperCase();
}

// human-friendly time-remaining string from an ISO expiry timestamp
function timeLeft(iso: string): string {
  const ms = new Date(iso).getTime() - Date.now();
  if (ms <= 0) return 'Expired';
  const hours = Math.floor(ms / 3_600_000);
  const mins = Math.floor((ms % 3_600_000) / 60_000);
  return hours > 0 ? `${hours}h ${mins}m left` : `${mins}m left`;
}

/* ═══════════════════════════════════════════════════════════
   App – root component for the VRS marketplace client
   ═══════════════════════════════════════════════════════════ */

function App() {
  /* ── identity & navigation state ── */
  const [auth, setAuth] = useState<AuthUser | null>(null);
  const [tab, setTab] = useState<Tab>('browse');
  const [status, setStatus] = useState('');

  /* ── data lists filled by API calls ── */
  const [videos, setVideos] = useState<Video[]>([]);
  const [rentals, setRentals] = useState<Rental[]>([]);
  const [studioVideos, setStudioVideos] = useState<Video[]>([]);

  /* ── browse filter inputs ── */
  const [keyword, setKeyword] = useState('');
  const [genre, setGenre] = useState('');
  const [maxPrice, setMaxPrice] = useState('');

  /* ── auth form inputs ── */
  const [authUser, setAuthUser] = useState('');
  const [authPass, setAuthPass] = useState('');
  const [authRole, setAuthRole] = useState<Role>('Customer');
  const [authStudio, setAuthStudio] = useState('');

  /* ── studio create-video form inputs ── */
  const [newId, setNewId] = useState('');
  const [newTitle, setNewTitle] = useState('');
  const [newGenre, setNewGenre] = useState('');
  const [newYear, setNewYear] = useState('');
  const [newType, setNewType] = useState('Movie');
  const [newPrice, setNewPrice] = useState('');
  const [newHours, setNewHours] = useState('');
  const [newPublished, setNewPublished] = useState(true);
  const [newOwner, setNewOwner] = useState('');

  // publisher and admin roles unlock the Studio tab
  const canStudio = auth?.role === 'Publisher' || auth?.role === 'Admin';

  /* ── restore session from localStorage on first mount ── */
  useEffect(() => {
    const raw = localStorage.getItem(AUTH_KEY);
    if (!raw) return;

    try {
      setAuth(JSON.parse(raw) as AuthUser);
    } catch {
      localStorage.removeItem(AUTH_KEY);
    }
  }, []);

  /* ── build URL query string from the three filter inputs ── */
  const browseQs = useMemo(() => {
    const p = new URLSearchParams();
    if (keyword.trim()) p.set('keyword', keyword.trim());
    if (genre.trim()) p.set('genre', genre.trim());
    if (maxPrice.trim()) p.set('maxPrice', maxPrice.trim());
    return p.toString();
  }, [keyword, genre, maxPrice]);

  /* ── fetch data for whichever tab is currently visible ── */
  const refresh = useCallback(async () => {
    try {
      if (tab === 'browse') {
        const qs = browseQs ? `?${browseQs}` : '';
        setVideos(await api<Video[]>(`/api/videos${qs}`));
      } else if (tab === 'rentals' && auth) {
        setRentals(await api<Rental[]>('/api/rentals/me', {}, auth.token));
      } else if (tab === 'studio' && auth && canStudio) {
        setStudioVideos(await api<Video[]>('/api/publisher/videos/me', {}, auth.token));
      }
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Request failed.');
    }
  }, [tab, auth, canStudio, browseQs]);

  // re-fetch whenever the active tab, identity, or filters change
  useEffect(() => { void refresh(); }, [refresh]);

  /* ═══════ auth handlers ═══════ */

  // shared helper that saves auth state to both React and localStorage
  function persistAuth(payload: AuthResponse) {
    const next: AuthUser = {
      token: payload.token,
      userId: payload.userId,
      username: payload.username,
      role: payload.role,
      studioName: payload.studioName,
    };

    localStorage.setItem(AUTH_KEY, JSON.stringify(next));
    setAuth(next);

    // publishers and admins land on the Studio tab after login
    if (next.role !== 'Customer') setTab('studio');
  }

  async function handleLogin(e: FormEvent) {
    e.preventDefault();
    try {
      const res = await api<AuthResponse>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username: authUser, password: authPass }),
      });
      persistAuth(res);
      setStatus(`Welcome back, ${res.username}.`);
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Login failed.');
    }
  }

  async function handleRegister(e: FormEvent) {
    e.preventDefault();
    try {
      const res = await api<AuthResponse>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({
          username: authUser,
          password: authPass,
          role: authRole,
          studioName: authStudio || undefined,
        }),
      });
      persistAuth(res);
      setStatus(`Account created — welcome, ${res.username}!`);
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Registration failed.');
    }
  }

  async function handleLogout() {
    if (!auth) return;

    // best-effort server-side token revocation
    try { await api('/api/auth/logout', { method: 'POST' }, auth.token); } catch { /* ignore */ }

    localStorage.removeItem(AUTH_KEY);
    setAuth(null);
    setTab('browse');
    setStatus('Signed out.');
  }

  /* ═══════ rental handlers ═══════ */

  async function handleRent(videoId: number) {
    if (!auth) { setStatus('Sign in to rent titles.'); return; }

    try {
      await api(`/api/rentals/${videoId}/rent`, { method: 'POST' }, auth.token);
      setStatus(`Rented title #${videoId}.`);
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Rent failed.');
    }
  }

  async function handleReturn(videoId: number) {
    if (!auth) return;

    try {
      await api(`/api/rentals/${videoId}/return`, { method: 'POST' }, auth.token);
      setStatus(`Returned title #${videoId}.`);
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Return failed.');
    }
  }

  /* ═══════ studio / publisher handlers ═══════ */

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!auth || !canStudio) { setStatus('Publisher or Admin required.'); return; }

    // parse numeric fields from the text inputs
    const vid = parseInt(newId, 10);
    const yr = parseInt(newYear, 10);
    const price = parseFloat(newPrice);
    const hrs = parseInt(newHours, 10);

    if ([vid, yr, price, hrs].some(Number.isNaN)) {
      setStatus('ID, year, price, and hours must be valid numbers.');
      return;
    }

    try {
      await api('/api/publisher/videos', {
        method: 'POST',
        body: JSON.stringify({
          videoId: vid,
          title: newTitle,
          genre: newGenre,
          releaseYear: yr,
          type: newType,
          rentalPrice: price,
          rentalHours: hrs,
          isPublished: newPublished,
          ownerPublisherId:
            auth.role === 'Admin' && newOwner.trim()
              ? parseInt(newOwner, 10)
              : undefined,
        }),
      }, auth.token);

      setStatus(`Title #${vid} created.`);
      // reset form fields after successful creation
      setNewId(''); setNewTitle(''); setNewGenre(''); setNewYear('');
      setNewPrice(''); setNewHours(''); setNewOwner('');
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Create failed.');
    }
  }

  async function handleDelete(videoId: number) {
    if (!auth || !canStudio) return;

    try {
      await api(`/api/publisher/videos/${videoId}`, { method: 'DELETE' }, auth.token);
      setStatus(`Deleted title #${videoId}.`);
      await refresh();
    } catch (err) {
      setStatus(err instanceof Error ? err.message : 'Delete failed.');
    }
  }

  /* ═══════ AI agent chat state ═══════ */

  const [chatOpen, setChatOpen] = useState(false);
  const [chatInput, setChatInput] = useState('');
  const [chatMsgs, setChatMsgs] = useState<ChatMsg[]>([]);
  const [chatLoading, setChatLoading] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);

  // auto-scroll chat to the latest message whenever new messages arrive
  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [chatMsgs]);

  async function handleChatSend(e: FormEvent) {
    e.preventDefault();
    const msg = chatInput.trim();
    if (!msg || chatLoading) return;

    // append the user message immediately for responsive feel
    setChatMsgs((prev) => [...prev, { role: 'user', text: msg }]);
    setChatInput('');
    setChatLoading(true);

    try {
      const res = await api<{ reply: string }>('/api/agent/chat', {
        method: 'POST',
        body: JSON.stringify({ message: msg }),
      }, auth?.token);

      setChatMsgs((prev) => [...prev, { role: 'agent', text: res.reply }]);

      // if the agent performed a rent/return, refresh the visible data
      if (/rented|returned/i.test(res.reply)) {
        await refresh();
      }
    } catch (err) {
      setChatMsgs((prev) => [...prev, {
        role: 'agent',
        text: err instanceof Error ? err.message : 'Agent request failed.',
      }]);
    } finally {
      setChatLoading(false);
    }
  }

  /* ═══════════════════════════════════════════
     JSX — builds the page top-to-bottom:
       navbar → auth panel → tabs → view → status → chat
     ═══════════════════════════════════════════ */

  return (
    <div className="app">

      {/* ── top navigation bar ── */}
      <header className="navbar">
        <div className="navbar-brand">
          <span className="logo">VRS</span>
          <span className="tagline">Pay-per-title marketplace</span>
        </div>

        <div className="navbar-right">
          {auth ? (
            <>
              <div className="user-pill">
                <span className="avatar">{initial(auth.username)}</span>
                {auth.username}
                <span className={`role-badge ${auth.role.toLowerCase()}`}>
                  {auth.role}
                </span>
              </div>
              <button className="btn btn-secondary btn-sm" onClick={handleLogout}>
                Sign out
              </button>
            </>
          ) : (
            <span className="user-pill">Guest</span>
          )}
          <a href="#/admin" className="admin-link">Admin</a>
        </div>
      </header>

      {/* ── auth modal: centered popup shown when not logged in ── */}
      {!auth && (
        <div className="auth-overlay">
          <div className="auth-modal">
            <div className="auth-modal-icon">
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/>
                <polyline points="10 17 15 12 10 7"/>
                <line x1="15" y1="12" x2="3" y2="12"/>
              </svg>
            </div>
            <h2 className="auth-modal-title">Welcome to VRS</h2>
            <p className="auth-modal-subtitle">Sign in to rent titles, or create a new account</p>

            <form className="auth-form" onSubmit={handleLogin}>
              <div className="auth-field">
                <label className="auth-label">Username</label>
                <input
                  className="input"
                  value={authUser}
                  onChange={(e) => setAuthUser(e.target.value)}
                  placeholder="Enter your username"
                  required
                />
              </div>
              <div className="auth-field">
                <label className="auth-label">Password</label>
                <input
                  className="input"
                  type="password"
                  value={authPass}
                  onChange={(e) => setAuthPass(e.target.value)}
                  placeholder="Enter your password"
                  required
                />
              </div>
              <div className="auth-field">
                <label className="auth-label">Account type</label>
                <select
                  className="input"
                  value={authRole}
                  onChange={(e) => setAuthRole(e.target.value as Role)}
                >
                  <option value="Customer">Customer</option>
                  <option value="Publisher">Publisher</option>
                </select>
              </div>
              {authRole === 'Publisher' && (
                <div className="auth-field">
                  <label className="auth-label">Studio name</label>
                  <input
                    className="input"
                    value={authStudio}
                    onChange={(e) => setAuthStudio(e.target.value)}
                    placeholder="Your studio or label"
                  />
                </div>
              )}
              <div className="auth-actions">
                <button className="btn btn-primary auth-btn-full" type="submit">Sign in</button>
                <button className="btn btn-secondary auth-btn-full" type="button" onClick={handleRegister}>
                  Create account
                </button>
              </div>
            </form>

            {status && <div className="auth-modal-status">{status}</div>}

            <div className="auth-modal-footer">
              <span className="auth-modal-hint">Demo accounts: <code>customer1</code> / <code>publisher1</code> / <code>admin</code></span>
            </div>
          </div>
        </div>
      )}

      {/* ── tab navigation ── */}
      <nav className="tab-bar">
        <button
          className={`tab-btn ${tab === 'browse' ? 'active' : ''}`}
          onClick={() => setTab('browse')}
        >
          Browse
        </button>
        <button
          className={`tab-btn ${tab === 'rentals' ? 'active' : ''}`}
          onClick={() => setTab('rentals')}
        >
          My Rentals
        </button>
        {canStudio && (
          <button
            className={`tab-btn ${tab === 'studio' ? 'active' : ''}`}
            onClick={() => setTab('studio')}
          >
            Studio
          </button>
        )}
      </nav>

      {/* ══════════ BROWSE TAB ══════════ */}
      {tab === 'browse' && (
        <section className="section">
          <div className="section-header">
            <h2 className="section-title">Browse Catalog</h2>
            <span className="section-count">
              {videos.length} title{videos.length !== 1 ? 's' : ''}
            </span>
          </div>

          {/* filter bar lets users narrow results by keyword, genre, or price */}
          <div className="filter-bar">
            <input
              className="input"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              placeholder="Search by keyword..."
            />
            <input
              className="input"
              value={genre}
              onChange={(e) => setGenre(e.target.value)}
              placeholder="Genre"
            />
            <input
              className="input"
              value={maxPrice}
              onChange={(e) => setMaxPrice(e.target.value)}
              placeholder="Max price"
              type="number"
              step="0.01"
              min="0"
            />
          </div>

          {videos.length === 0 ? (
            <div className="empty-state">
              <div className="empty-icon">🎬</div>
              <p>No titles match your filters.</p>
            </div>
          ) : (
            <div className="card-grid">
              {videos.map((v) => (
                <article className="video-card" key={v.videoId}>
                  <div className="card-type">{v.type}</div>
                  <div className="card-title">{v.title}</div>
                  <div className="card-meta">
                    {v.genre} &middot; {v.releaseYear}
                  </div>
                  <div className="card-bottom">
                    <span className="price-tag">
                      ${v.rentalPrice.toFixed(2)}
                      <span className="duration"> / {v.rentalHours}h</span>
                    </span>
                    {v.isRented ? (
                      <span className="availability-badge rented">Rented</span>
                    ) : (
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() => handleRent(v.videoId)}
                      >
                        Rent now
                      </button>
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      )}

      {/* ══════════ RENTALS TAB ══════════ */}
      {tab === 'rentals' && (
        <section className="section">
          <div className="section-header">
            <h2 className="section-title">My Rentals</h2>
            {auth && (
              <span className="section-count">
                {rentals.length} active
              </span>
            )}
          </div>

          {!auth ? (
            <div className="empty-state">
              <div className="empty-icon">🔑</div>
              <p>Sign in to view your rented titles.</p>
            </div>
          ) : rentals.length === 0 ? (
            <div className="empty-state">
              <div className="empty-icon">📭</div>
              <p>You haven't rented anything yet. Browse the catalog!</p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {rentals.map((r) => (
                <div className="rental-card" key={r.videoId}>
                  <div className="rental-info">
                    <div className="rental-title">{r.title}</div>
                    <div className="rental-meta">
                      {r.genre} &middot; <span>{timeLeft(r.expiryUtc)}</span>
                    </div>
                  </div>
                  <div className="rental-right">
                    <span className="rental-paid">
                      ${r.paidAmount.toFixed(2)}
                    </span>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => handleReturn(r.videoId)}
                    >
                      Return
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {/* ══════════ STUDIO TAB ══════════ */}
      {tab === 'studio' && canStudio && (
        <section className="section">
          <div className="section-header">
            <h2 className="section-title">Studio Dashboard</h2>
            <span className="section-count">
              {studioVideos.length} title{studioVideos.length !== 1 ? 's' : ''}
            </span>
          </div>

          {/* create-title form for publishers/admins */}
          <form className="create-form" onSubmit={handleCreate}>
            <input className="input" value={newId} onChange={(e) => setNewId(e.target.value)} placeholder="Video ID" required />
            <input className="input" value={newTitle} onChange={(e) => setNewTitle(e.target.value)} placeholder="Title" required />
            <input className="input" value={newGenre} onChange={(e) => setNewGenre(e.target.value)} placeholder="Genre" required />
            <input className="input" value={newYear} onChange={(e) => setNewYear(e.target.value)} placeholder="Year" required />
            <select className="input" value={newType} onChange={(e) => setNewType(e.target.value)}>
              <option value="Movie">Movie</option>
              <option value="Series">Series</option>
            </select>
            <input className="input" value={newPrice} onChange={(e) => setNewPrice(e.target.value)} placeholder="Price" required />
            <input className="input" value={newHours} onChange={(e) => setNewHours(e.target.value)} placeholder="Rental hours" required />

            {auth?.role === 'Admin' && (
              <input
                className="input"
                value={newOwner}
                onChange={(e) => setNewOwner(e.target.value)}
                placeholder="Owner publisher ID"
              />
            )}

            <div className="form-full">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={newPublished}
                  onChange={(e) => setNewPublished(e.target.checked)}
                />
                Publish immediately
              </label>
              <button className="btn btn-primary" type="submit">Create title</button>
            </div>
          </form>

          {studioVideos.length === 0 ? (
            <div className="empty-state">
              <div className="empty-icon">🎥</div>
              <p>No titles in your catalog yet. Create one above!</p>
            </div>
          ) : (
            <div className="card-grid">
              {studioVideos.map((v) => (
                <article className="video-card" key={v.videoId}>
                  <div className="card-type">
                    {v.type}
                    {!v.isPublished && (
                      <span style={{ marginLeft: 8, color: 'var(--text-muted)', fontStyle: 'italic' }}>
                        Draft
                      </span>
                    )}
                  </div>
                  <div className="card-title">{v.title}</div>
                  <div className="card-meta">
                    {v.genre} &middot; {v.releaseYear} &middot; ID #{v.videoId}
                  </div>
                  <div className="card-bottom">
                    <span className="price-tag">
                      ${v.rentalPrice.toFixed(2)}
                      <span className="duration"> / {v.rentalHours}h</span>
                    </span>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => handleDelete(v.videoId)}
                    >
                      Delete
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      )}

      {/* ── persistent status bar at the bottom ── */}
      <div className="status-bar">
        <span className="status-dot" />
        {status || 'Ready.'}
      </div>

      {/* ══════════ AI AGENT CHAT DRAWER ══════════ */}

      {/* floating action button to toggle the chat panel */}
      <button
        className="chat-fab"
        onClick={() => setChatOpen((o) => !o)}
        title="AI Assistant"
      >
        {chatOpen ? '✕' : '🤖'}
      </button>

      {chatOpen && (
        <aside className="chat-drawer">
          <div className="chat-header">
            <span className="chat-header-icon">🤖</span>
            <span className="chat-header-title">VRS AI Assistant</span>
          </div>

          <div className="chat-messages">
            {chatMsgs.length === 0 && (
              <div className="chat-welcome">
                <p>Hi! I'm the VRS AI Assistant.</p>
                <p>Try asking me:</p>
                <div className="chat-suggestions">
                  {['Recommend something', 'Search for sci-fi', 'Stats', 'My rentals'].map((s) => (
                    <button
                      key={s}
                      className="chat-suggestion"
                      onClick={() => { setChatInput(s); }}
                    >
                      {s}
                    </button>
                  ))}
                </div>
              </div>
            )}
            {chatMsgs.map((m, i) => (
              <div key={i} className={`chat-bubble ${m.role}`}>
                {m.text.split('\n').map((line, j) => (
                  <div key={j}>{line || '\u00A0'}</div>
                ))}
              </div>
            ))}
            {chatLoading && (
              <div className="chat-bubble agent typing">Thinking...</div>
            )}
            <div ref={chatEndRef} />
          </div>

          <form className="chat-input-row" onSubmit={handleChatSend}>
            <input
              className="input chat-input"
              value={chatInput}
              onChange={(e) => setChatInput(e.target.value)}
              placeholder="Ask the AI assistant..."
              disabled={chatLoading}
            />
            <button
              className="btn btn-primary btn-sm"
              type="submit"
              disabled={chatLoading || !chatInput.trim()}
            >
              Send
            </button>
          </form>
        </aside>
      )}
    </div>
  );
}

export default App;
