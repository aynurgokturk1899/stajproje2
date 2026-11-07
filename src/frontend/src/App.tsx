// The existing UI is migrated incrementally; runtime behavior remains covered by the API contract.
// @ts-nocheck
import { useEffect, useRef, useState } from 'react';
import { changePassword, login, register, request, uploadAttachment } from './api';
import './auth.css';
import './list.css';
import './account.css';
import './modal.css';

const cards = [
  ['Aktif Projeler', 'activeProjectCount', '🗂️', 'projects', 'Çalışma alanındaki projeler'],
  ['Açık Görevler', 'openTaskCount', '✓', 'tasks', 'Tamamlanmayı bekleyen işler'],
  ['Bana Atananlar', 'myOpenTaskCount', '👤', 'tasks', 'Sana ait açık görevler'],
  ['Okunmamış Bildirim', 'unreadNotificationCount', '🔔', 'notifications', 'Yeni gelişmeleri kontrol et']
];

export default function App() {
  const [session, setSession] = useState(() => JSON.parse(localStorage.getItem('session') ?? 'null'));
  const [summary, setSummary] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [isRegistering, setIsRegistering] = useState(false);
  const [page, setPage] = useState('dashboard');
  const [items, setItems] = useState([]);
  const [adminOverview, setAdminOverview] = useState({ users: [], projects: [] });
  const [showProjectForm, setShowProjectForm] = useState(false);
  const [showUserForm, setShowUserForm] = useState(false);
  const [memberProject, setMemberProject] = useState(null);
  const [projectMembers, setProjectMembers] = useState([]);
  const [eligibleUsers, setEligibleUsers] = useState([]);
  const [showTaskForm, setShowTaskForm] = useState(false);
  const [taskProjects, setTaskProjects] = useState([]);
  const [taskMembers, setTaskMembers] = useState([]);
  const [activeUsers, setActiveUsers] = useState([]);
  const [projectManagers, setProjectManagers] = useState([]);
  const [taskStatusFilter, setTaskStatusFilter] = useState('');
  const [taskPriorityFilter, setTaskPriorityFilter] = useState('');
  const [taskSort, setTaskSort] = useState('createddate:desc');
  const [taskPageNumber, setTaskPageNumber] = useState(1);
  const [taskPage, setTaskPage] = useState({ pageNumber: 1, totalPages: 1, totalCount: 0 });
  const [editTask, setEditTask] = useState(null);
  const [profile, setProfile] = useState(null);
  const [detailTask, setDetailTask] = useState(null);
  const [comments, setComments] = useState([]);
  const [attachments, setAttachments] = useState([]);
  const [unreadNotificationCount, setUnreadNotificationCount] = useState(0);
  const [notificationToast, setNotificationToast] = useState(null);
  const lastNotificationId = useRef(null);
  const isAdmin = session?.roles?.includes('Admin');
  const canManageProjects = isAdmin || session?.roles?.includes('ProjectManager');
  const canViewProjects = Boolean(session);

  useEffect(() => {
    const handleExpiredSession = () => { setSession(null); setProfile(null); setError('Oturum süreniz doldu. Lütfen yeniden giriş yapın.'); };
    window.addEventListener('session-expired', handleExpiredSession);
    return () => window.removeEventListener('session-expired', handleExpiredSession);
  }, []);

  const loadNotificationIndicator = async () => {
    try {
      const [dashboard, notifications] = await Promise.all([
        request('/api/dashboard/summary'),
        request('/api/notifications?pageSize=5')
      ]);
      setUnreadNotificationCount(dashboard.unreadNotificationCount ?? 0);
      const latest = notifications.items?.[0];
      if (latest && lastNotificationId.current !== null && latest.id !== lastNotificationId.current && !latest.isRead) {
        setNotificationToast(latest);
        window.setTimeout(() => setNotificationToast(current => current?.id === latest.id ? null : current), 6000);
      }
      if (latest) lastNotificationId.current = latest.id;
    } catch { /* Sayfa hatasını bildirim kontrolüyle değiştirme. */ }
  };

  useEffect(() => {
    if (!session) return;
    loadNotificationIndicator();
    const timer = window.setInterval(loadNotificationIndicator, 15000);
    return () => window.clearInterval(timer);
  }, [session]);

  const loadDashboard = async () => {
    setLoading(true);
    try {
      const [dashboard, progress, workload, todayTasks] = await Promise.all([
        request('/api/dashboard/summary'),
        request('/api/reports/project-progress'),
        request('/api/reports/workload'),
        request('/api/tasks/my?pageSize=5&sort=duedate:asc')
      ]);
      setSummary({ dashboard, progress, workload, todayTasks: todayTasks.items ?? [] });
      setError('');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  const loadPage = async () => {
    if (page === 'dashboard') return loadDashboard();
    if (page === 'account') {
      setLoading(true);
      try { setProfile(await request('/api/auth/profile')); setError(''); }
      catch (exception) { setError(exception.message); }
      finally { setLoading(false); }
      return;
    }
    setLoading(true);
    try {
      if (page === 'admin') {
        const [users, projects] = await Promise.all([
          request('/api/users?pageSize=100&isActive=true'),
          request('/api/projects?pageSize=100')
        ]);
        setAdminOverview({ users: users.items ?? [], projects: projects.items ?? [] });
        setError('');
        return;
      }
      if (page === 'projects' && !canManageProjects) {
        const [myProjects, availableProjects] = await Promise.all([
          request('/api/projects?pageSize=50'),
          request('/api/projects?pageSize=50&availableForJoin=true')
        ]);
        setItems([
          ...(myProjects.items ?? []).map(project => ({ ...project, membershipView: 'member' })),
          ...(availableProjects.items ?? []).map(project => ({ ...project, membershipView: 'available' }))
        ]);
        setError('');
        return;
      }
      const endpoint = page === 'tasks' ? `/api/tasks${canManageProjects ? '' : '/my'}?pageNumber=${taskPageNumber}&pageSize=10&sort=${encodeURIComponent(taskSort)}${taskStatusFilter ? `&status=${taskStatusFilter}` : ''}${taskPriorityFilter ? `&priority=${taskPriorityFilter}` : ''}` : page === 'notifications' ? '/api/notifications?pageSize=50' : page === 'requests' ? '/api/project-join-requests/pending' : page === 'myRequests' ? '/api/project-join-requests/my' : `/api/projects?pageSize=50${canManageProjects ? '' : '&availableForJoin=true'}`;
      const data = await request(endpoint);
      setItems(data.items ?? data ?? []);
      if (page === 'tasks') setTaskPage({ pageNumber: data.pageNumber, totalPages: data.totalPages, totalCount: data.totalCount });
      setError('');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { if (session) loadPage(); }, [session, page, taskStatusFilter, taskPriorityFilter, taskSort, taskPageNumber]);

  const handleLogin = async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setLoading(true);
    try {
      const result = await login(form.get('email'), form.get('password'));
      localStorage.setItem('accessToken', result.accessToken);
      localStorage.setItem('session', JSON.stringify(result.user));
      setSession(result.user);
      setError('');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    if (form.get('password') !== form.get('confirmPassword')) {
      setError('Parolalar eşleşmiyor.');
      return;
    }
    setLoading(true);
    try {
      const result = await register({ firstName: form.get('firstName'), lastName: form.get('lastName'), email: form.get('email'), password: form.get('password') });
      localStorage.setItem('accessToken', result.accessToken);
      localStorage.setItem('session', JSON.stringify(result.user));
      setSession(result.user);
      setError('');
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const sendJoinRequest = async (projectId) => {
    setLoading(true);
    try { await request(`/api/projects/${projectId}/join-requests`, { method: 'POST' }); await loadPage(); }
    catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const reviewJoinRequest = async (id, approve) => {
    setLoading(true);
    try { await request(`/api/project-join-requests/${id}/${approve ? 'approve' : 'reject'}`, { method: 'PATCH' }); setItems(current => current.filter(item => item.id !== id)); }
    catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const updateTaskStatus = async (taskId, status) => {
    setLoading(true);
    try {
      await request(`/api/tasks/${taskId}/status`, { method: 'PATCH', body: JSON.stringify({ status: Number(status) }) });
      setItems(current => current.map(item => item.id === taskId ? { ...item, status: Number(status) } : item));
      setError('');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  const markNotificationRead = async (notificationId) => {
    try {
      await request(`/api/notifications/${notificationId}/read`, { method: 'PATCH' });
      setItems(current => current.map(item => item.id === notificationId ? { ...item, isRead: true } : item));
      setUnreadNotificationCount(current => Math.max(0, current - 1));
    } catch (exception) { setError(exception.message); }
  };

  const openNotificationTarget = async (notification) => {
    setNotificationToast(null);
    try {
      if (!notification.isRead) await markNotificationRead(notification.id);
      const type = Number(notification.type);
      if (notification.relatedEntityId && type >= 1 && type <= 4) {
        const task = await request(`/api/tasks/${notification.relatedEntityId}`);
        setPage('tasks');
        await openTaskDetail(task);
      } else if (notification.relatedEntityId && type === 5) {
        setPage('projects');
      } else {
        setPage('notifications');
      }
    } catch (exception) { setError(exception.message); }
  };

  const markAllNotificationsRead = async () => {
    try {
      await request('/api/notifications/read-all', { method: 'PATCH' });
      setItems(current => current.map(item => ({ ...item, isRead: true })));
      setUnreadNotificationCount(0);
    } catch (exception) { setError(exception.message); }
  };

  const handleChangePassword = async (event) => {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    if (form.get('newPassword') !== form.get('confirmPassword')) {
      setError('Yeni parolalar eşleşmiyor.');
      return;
    }
    setLoading(true);
    try {
      await changePassword(form.get('currentPassword'), form.get('newPassword'));
      formElement.reset();
      setError('');
      alert('Parolan başarıyla güncellendi.');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateProfile = async (event) => {
    event.preventDefault(); const form = new FormData(event.currentTarget); setLoading(true);
    try { const updated = await request('/api/auth/profile', { method: 'PUT', body: JSON.stringify({ firstName: form.get('firstName'), lastName: form.get('lastName'), phoneNumber: form.get('phoneNumber') || null }) }); setProfile(updated); const nextSession = { ...session, firstName: updated.firstName, lastName: updated.lastName }; localStorage.setItem('session', JSON.stringify(nextSession)); setSession(nextSession); setError(''); alert('Profil güncellendi.'); }
    catch (exception) { setError(exception.message); } finally { setLoading(false); }
  };

  const handleCreateProject = async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setLoading(true);
    try {
      const project = await request('/api/projects', { method: 'POST', body: JSON.stringify({ name: form.get('name'), description: form.get('description') || null, startDate: form.get('startDate'), targetEndDate: form.get('targetEndDate'), managerUserId: isAdmin ? form.get('managerUserId') : null }) });
      setItems(current => [project, ...current]);
      setShowProjectForm(false);
      setError('');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateUser = async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    if (form.get('password') !== form.get('confirmPassword')) { setError('Parolalar eşleşmiyor.'); return; }
    setLoading(true);
    try {
      const user = await request('/api/users', { method: 'POST', body: JSON.stringify({ firstName: form.get('firstName'), lastName: form.get('lastName'), email: form.get('email'), password: form.get('password'), roles: [form.get('role')] }) });
      setAdminOverview(current => ({ ...current, users: [user, ...current.users] }));
      setShowUserForm(false); setError(''); alert('Kullanıcı başarıyla oluşturuldu.');
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const openMemberManager = async (project) => {
    setLoading(true);
    try {
      const members = await request(`/api/projects/${project.id}/members`);
      let candidates;
      try {
        candidates = await request(`/api/projects/${project.id}/members/eligible-users`);
      } catch (candidateError) {
        if (!isAdmin) throw candidateError;
        const users = await request('/api/users?pageSize=100&isActive=true');
        const activeMemberIds = new Set(members.filter(member => member.isActive).map(member => member.userId));
        candidates = (users.items ?? []).filter(user => !activeMemberIds.has(user.id));
      }
      setMemberProject(project); setProjectMembers(members); setEligibleUsers(candidates); setError('');
    } catch (exception) { setError(`Üye yönetimi açılamadı: ${exception.message}`); }
    finally { setLoading(false); }
  };

  const handleAddProjectMember = async (event) => {
    event.preventDefault(); const form = new FormData(event.currentTarget); setLoading(true);
    try {
      const member = await request(`/api/projects/${memberProject.id}/members`, { method: 'POST', body: JSON.stringify({ userId: form.get('userId'), memberRole: 2 }) });
      setProjectMembers(current => [...current, member]);
      setEligibleUsers(current => current.filter(user => user.id !== member.userId));
      setError(''); alert(`${member.firstName} ${member.lastName} projeye eklendi.`);
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const removeProjectMember = async (member) => {
    if (!confirm(`${member.firstName} ${member.lastName} projeden çıkarılsın mı?`)) return;
    setLoading(true);
    try {
      await request(`/api/projects/${memberProject.id}/members/${member.userId}`, { method: 'DELETE' });
      setProjectMembers(current => current.filter(item => item.userId !== member.userId));
      setEligibleUsers(await request(`/api/projects/${memberProject.id}/members/eligible-users`)); setError('');
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const openProjectForm = async () => {
    setLoading(true);
    try {
      if (isAdmin) {
        const data = await request('/api/users?pageSize=100&isActive=true');
        setProjectManagers(data.items ?? []);
      }
      setShowProjectForm(true);
      setError('');
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const openTaskForm = async () => {
    setLoading(true);
    try {
      const [data, users] = await Promise.all([
        request('/api/projects?pageSize=100'),
        isAdmin ? request('/api/users?pageSize=100&isActive=true') : Promise.resolve({ items: [] })
      ]);
      const projects = data.items ?? [];
      setTaskProjects(projects);
      setActiveUsers(users.items ?? []);
      setTaskMembers(projects.length ? await request(`/api/projects/${projects[0].id}/members`) : []);
      setShowTaskForm(true);
      setError('');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  const loadTaskMembers = async (projectId) => {
    if (!projectId) { setTaskMembers([]); return; }
    try {
      setTaskMembers(await request(`/api/projects/${projectId}/members`));
      setError('');
    } catch (exception) { setTaskMembers([]); setError(exception.message); }
  };

  const handleCreateTask = async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setLoading(true);
    try {
      const projectId = Number(form.get('projectId'));
      const assignedUserId = String(form.get('assignedUserId'));
      const isAlreadyMember = taskMembers.some(member => member.userId === assignedUserId && member.isActive);
      if (isAdmin && !isAlreadyMember) {
        await request(`/api/projects/${projectId}/members`, { method: 'POST', body: JSON.stringify({ userId: assignedUserId, memberRole: 2 }) });
      }
      const task = await request('/api/tasks', { method: 'POST', body: JSON.stringify({ projectId, title: form.get('title'), description: form.get('description') || null, assignedUserId, priority: Number(form.get('priority')), startDate: form.get('startDate') || null, dueDate: form.get('dueDate') }) });
      setItems(current => [task, ...current]);
      setShowTaskForm(false);
      setError('');
    } catch (exception) {
      setError(exception.message);
    } finally {
      setLoading(false);
    }
  };

  const openTaskDetail = async (task) => {
    setLoading(true);
    try {
      const [taskComments, taskAttachments] = await Promise.all([request(`/api/tasks/${task.id}/comments`), request(`/api/tasks/${task.id}/attachments`)]);
      setComments(taskComments);
      setAttachments(taskAttachments);
      setDetailTask(task);
      setError('');
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const openTaskEdit = async (task) => {
    setLoading(true);
    try {
      setTaskMembers(await request(`/api/projects/${task.projectId}/members`));
      setEditTask(task);
      setError('');
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const handleUpdateTask = async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setLoading(true);
    try {
      const updated = await request(`/api/tasks/${editTask.id}`, { method: 'PUT', body: JSON.stringify({ title: form.get('title'), description: form.get('description') || null, assignedUserId: form.get('assignedUserId'), priority: Number(form.get('priority')), startDate: form.get('startDate') || null, dueDate: form.get('dueDate') }) });
      setItems(current => current.map(item => item.id === updated.id ? updated : item));
      setEditTask(null);
      setError('');
    } catch (exception) { setError(exception.message); }
    finally { setLoading(false); }
  };

  const addComment = async (event) => {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    try {
      const comment = await request(`/api/tasks/${detailTask.id}/comments`, { method: 'POST', body: JSON.stringify({ content: form.get('content') }) });
      setComments(current => [...current, comment]);
      formElement.reset();
    } catch (exception) { setError(exception.message); }
  };

  const uploadFile = async (event) => {
    event.preventDefault();
    const formElement = event.currentTarget;
    const file = formElement.file.files[0];
    if (!file) return;
    try {
      const attachment = await uploadAttachment(detailTask.id, file);
      setAttachments(current => [...current, attachment]);
      formElement.reset();
    } catch (exception) { setError(exception.message); }
  };

  if (!session) return <LoginForm isRegistering={isRegistering} onSubmit={isRegistering ? handleRegister : handleLogin} onSwitch={() => { setError(''); setIsRegistering(!isRegistering); }} error={error} loading={loading} />;

  return <main className="app-shell">
    <aside className="sidebar">
      <div className="brand"><span>✓</span> GörevAkış</div>
      <nav><a className={page === 'dashboard' ? 'active' : ''} onClick={() => setPage('dashboard')}>▦ Genel Bakış</a>{isAdmin && <a className={page === 'admin' ? 'active' : ''} onClick={() => setPage('admin')}>♛ Yönetim Paneli</a>}{canViewProjects && <a className={page === 'projects' ? 'active' : ''} onClick={() => setPage('projects')}>◫ Projeler</a>} {!canManageProjects && <a className={page === 'myRequests' ? 'active' : ''} onClick={() => setPage('myRequests')}>◌ Katılım isteklerim</a>}{canManageProjects && <a className={page === 'requests' ? 'active' : ''} onClick={() => setPage('requests')}>◌ Katılım istekleri</a>}<a className={page === 'tasks' ? 'active' : ''} onClick={() => setPage('tasks')}>☑ Görevlerim</a><a className={page === 'notifications' ? 'active' : ''} onClick={() => setPage('notifications')}><span>◌ Bildirimler</span>{unreadNotificationCount > 0 && <b className="nav-badge">{unreadNotificationCount > 99 ? '99+' : unreadNotificationCount}</b>}</a><a className={page === 'account' ? 'active' : ''} onClick={() => setPage('account')}>⚙ Hesabım</a></nav>
      <button className="logout" onClick={() => { localStorage.clear(); setSession(null); }}>Çıkış yap</button>
    </aside>
    <section className="content">
      <header><div><p className="eyebrow">EKİP ÇALIŞMA ALANI</p><h1>{page === 'dashboard' ? `Hoş geldin, ${session.firstName} 👋` : page === 'admin' ? 'Yönetim Paneli' : page === 'tasks' ? 'Görevlerim' : page === 'notifications' ? 'Bildirimler' : page === 'account' ? 'Hesabım' : page === 'requests' ? 'Katılım istekleri' : page === 'myRequests' ? 'Katılım isteklerim' : 'Projeler'}</h1><p>{page === 'admin' ? 'Ekip üyelerini oluştur, incele ve proje yöneticisi ata.' : page === 'projects' && canManageProjects ? 'Projeleri ve ekip üyelerini yönet.' : page === 'projects' ? 'Uygun projeye katılım isteği gönder.' : page === 'requests' ? 'Yönettiğin projelere gelen istekleri değerlendir.' : page === 'myRequests' ? 'Gönderdiğin isteklerin durumunu takip et.' : 'Çalışma alanını buradan takip et.'}</p></div><div className="header-actions">{page === 'admin' && isAdmin && <button onClick={() => setShowUserForm(true)}>+ Ekip üyesi oluştur</button>}{(page === 'projects' || page === 'admin') && isAdmin && <button onClick={openProjectForm}>+ Proje oluştur</button>}{page === 'projects' && !isAdmin && canManageProjects && <button onClick={openProjectForm}>+ Proje oluştur</button>}{page === 'tasks' && <select className="task-filter" value={taskStatusFilter} onChange={event => { setTaskStatusFilter(event.target.value); setTaskPageNumber(1); }}><option value="">Tüm durumlar</option><option value="1">Yeni</option><option value="2">Devam ediyor</option><option value="3">Beklemede</option><option value="4">Tamamlandı</option><option value="5">İptal edildi</option></select>}{page === 'tasks' && <select className="task-filter" value={taskPriorityFilter} onChange={event => { setTaskPriorityFilter(event.target.value); setTaskPageNumber(1); }}><option value="">Tüm öncelikler</option><option value="1">Düşük</option><option value="2">Orta</option><option value="3">Yüksek</option><option value="4">Kritik</option></select>}{page === 'tasks' && <select className="task-filter" value={taskSort} onChange={event => { setTaskSort(event.target.value); setTaskPageNumber(1); }}><option value="createddate:desc">En yeni</option><option value="createddate:asc">En eski</option><option value="duedate:asc">Bitiş tarihi ↑</option><option value="duedate:desc">Bitiş tarihi ↓</option></select>}{page === 'tasks' && canManageProjects && <button onClick={openTaskForm}>+ Görev oluştur</button>}{page === 'notifications' && <button className="refresh" onClick={markAllNotificationsRead}>Tümünü oku</button>}{page !== 'account' && <button className="refresh" onClick={loadPage}>↻ Yenile</button>}</div></header>
      {error && <div className="alert">{error}</div>}
      {page === 'dashboard' ? <><section className="dashboard-hero"><div className="dashboard-profile"><span className="dashboard-avatar">{session.firstName?.[0]}{session.lastName?.[0]}</span><div><b>{session.firstName} {session.lastName}</b><small>{session.roles?.join(' · ')}</small></div></div><div className="dashboard-date"><span>BUGÜN</span><b>{new Intl.DateTimeFormat('tr-TR', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date())}</b></div></section><div className="cards">{cards.map(([label, key, icon, targetPage, detail]) => <article className="card clickable-card" key={key} role="button" tabIndex="0" onClick={() => setPage(targetPage)} onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') setPage(targetPage); }}><span className="icon">{icon}</span><p>{label}</p><strong>{summary?.dashboard?.[key] ?? (loading ? '…' : '0')}</strong><small className="card-detail">{detail}</small></article>)}</div>
      <section className="panel today-panel"><div className="panel-heading"><div><h2>Bugünün görevleri</h2><p>Öncelik vermen gereken en yakın işler</p></div><button className="mini-button" onClick={() => setPage('tasks')}>Tüm görevler</button></div>{summary?.todayTasks?.length ? <div className="today-list">{summary.todayTasks.map(task => <button className="today-task" key={task.id} onClick={() => setPage('tasks')}><span className="today-task-icon">{task.priority >= 3 ? '!' : '✓'}</span><span><b>{task.title}</b><small>Son tarih: {task.dueDate} · {priorityText(task.priority)}</small></span><i>{statusText(task.status)}</i></button>)}</div> : <p className="muted">Bugün için sana atanmış açık görev bulunmuyor.</p>}</section>
      <section className="panel"><div className="panel-heading"><div><h2>Proje ilerlemesi</h2><p>Görevlerin tamamlanma durumu</p></div></div>
        {loading && !summary ? <p className="muted">Veriler yükleniyor…</p> : summary?.progress?.length ? <div className="progress-list">{summary.progress.map(project => <div className="progress-item" key={project.projectId}><div><strong>{project.projectName}</strong><span>{project.completedTaskCount} / {project.totalTaskCount} görev tamamlandı</span></div><div className="bar"><i style={{ width: `${project.completionPercentage}%` }} /></div><b>%{project.completionPercentage}</b></div>)}</div> : <p className="muted">Henüz görüntülenecek proje bulunmuyor.</p>}
      </section><section className="panel workload-panel"><div className="panel-heading"><div><h2>İş yükü dağılımı</h2><p>Atanan görevlerin ekip bazlı görünümü</p></div></div>{summary?.workload?.length ? <div className="workload-table"><div className="workload-head"><span>Kullanıcı</span><span>Açık görev</span><span>Toplam görev</span></div>{summary.workload.map(user => <div className="workload-row" key={user.userId}><span><b>{user.firstName} {user.lastName}</b><small>{user.email}</small></span><span>{user.openTaskCount}</span><span>{user.totalTaskCount}</span></div>)}</div> : <p className="muted">Henüz iş yükü verisi bulunmuyor.</p>}</section></> : page === 'admin' ? <AdminPanel overview={adminOverview} loading={loading} /> : page === 'account' ? <AccountPanel session={session} profile={profile} onProfileSubmit={handleUpdateProfile} onPasswordSubmit={handleChangePassword} loading={loading} /> : <ListPanel page={page} items={items} loading={loading} canJoin={!canManageProjects} canManageMembers={canManageProjects} canEditTasks={canManageProjects} onStatusChange={updateTaskStatus} onNotificationOpen={openNotificationTarget} onTaskDetail={openTaskDetail} onTaskEdit={openTaskEdit} onManageMembers={openMemberManager} onJoin={sendJoinRequest} onReview={reviewJoinRequest} />}
      {showUserForm && <UserModal onClose={() => setShowUserForm(false)} onSubmit={handleCreateUser} loading={loading} />}
      {showProjectForm && <ProjectModal isAdmin={isAdmin} managers={projectManagers} onClose={() => setShowProjectForm(false)} onSubmit={handleCreateProject} loading={loading} />}
      {memberProject && <MemberModal project={memberProject} members={projectMembers} eligibleUsers={eligibleUsers} onClose={() => setMemberProject(null)} onSubmit={handleAddProjectMember} onRemove={removeProjectMember} loading={loading} />}
      {showTaskForm && <TaskModal projects={taskProjects} members={isAdmin ? activeUsers : taskMembers} adminAutoMembership={isAdmin} onProjectChange={loadTaskMembers} onClose={() => setShowTaskForm(false)} onSubmit={handleCreateTask} loading={loading} />}
      {editTask && <EditTaskModal task={editTask} members={taskMembers} onClose={() => setEditTask(null)} onSubmit={handleUpdateTask} loading={loading} />}
      {detailTask && <TaskDetailModal task={detailTask} comments={comments} attachments={attachments} onClose={() => setDetailTask(null)} onSubmit={addComment} onUpload={uploadFile} />}
      {page === 'tasks' && <nav className="pagination" aria-label="Görev sayfaları"><button disabled={taskPage.pageNumber <= 1 || loading} onClick={() => setTaskPageNumber(value => value - 1)}>← Önceki</button><span>Sayfa {taskPage.pageNumber} / {Math.max(1, taskPage.totalPages)} · {taskPage.totalCount} görev</span><button disabled={taskPage.pageNumber >= taskPage.totalPages || loading} onClick={() => setTaskPageNumber(value => value + 1)}>Sonraki →</button></nav>}
    </section>
    {notificationToast && <button className="notification-toast" onClick={() => openNotificationTarget(notificationToast)}><span>🔔</span><span><b>{notificationToast.title}</b><small>{notificationToast.message}</small></span><i>Aç</i></button>}
  </main>;
}

function AccountPanel({ session, profile, onProfileSubmit, onPasswordSubmit, loading }) {
  if (!profile) return <section className="panel"><p className="muted">Profil yükleniyor…</p></section>;
  return <div className="account-page"><div className="account-grid"><section className="panel account-summary"><div className="avatar">{profile.firstName?.[0]}{profile.lastName?.[0]}</div><h2>{profile.firstName} {profile.lastName}</h2><p>{profile.email}</p><span className="role-tag">{profile.roles?.join(' · ')}</span><small>Üyelik: {new Date(profile.createdDate).toLocaleDateString('tr-TR')} · {profile.isActive ? 'Aktif' : 'Pasif'}</small></section><section className="panel profile-edit"><h2>Kişisel bilgiler</h2><form onSubmit={onProfileSubmit}><div className="name-fields"><label>Ad<input required name="firstName" defaultValue={profile.firstName} /></label><label>Soyad<input required name="lastName" defaultValue={profile.lastName} /></label></div><label>Telefon<input name="phoneNumber" defaultValue={profile.phoneNumber ?? ''} placeholder="05xx xxx xx xx" /></label><button disabled={loading}>Bilgileri kaydet</button></form></section></div><section className="profile-stats"><article><b>{profile.projectCount}</b><span>Proje</span></article><article><b>{profile.openTaskCount}</b><span>Açık görev</span></article><article><b>{profile.completedTaskCount}</b><span>Tamamlanan</span></article><article><b>{profile.overdueTaskCount}</b><span>Geciken</span></article></section><div className="account-grid lower"><section className="panel activities"><h2>Son aktiviteler</h2>{profile.recentActivities.length ? profile.recentActivities.map(item => <div className="activity" key={item.id}><b>{item.description}</b>{item.entityName && <span className="activity-entity">{item.entityType === 'TaskItem' ? `Görev: ${item.entityName}` : `Proje: ${item.entityName}`}{item.entityType === 'TaskItem' && item.projectName ? ` · Proje: ${item.projectName}` : ''}</span>}<span>{new Date(item.createdDate).toLocaleString('tr-TR')}</span></div>) : <p className="muted">Henüz aktivite yok.</p>}</section><section className="panel password-panel"><h2>Güvenlik</h2><p className="muted">Parolanı buradan değiştirebilirsin.</p><form onSubmit={onPasswordSubmit}><label>Mevcut parola<input required name="currentPassword" type="password" /></label><label>Yeni parola<input required minLength="8" name="newPassword" type="password" /></label><label>Yeni parola tekrarı<input required name="confirmPassword" type="password" /></label><button disabled={loading}>Parolayı güncelle</button></form></section></div></div>;
}

function UserModal({ onClose, onSubmit, loading }) {
  return <div className="modal-backdrop"><section className="modal"><div className="modal-header"><div><h2>Yeni ekip üyesi</h2><p className="muted">Kullanıcı giriş bilgilerini ve rolünü belirle.</p></div><button className="close-button" type="button" onClick={onClose}>×</button></div><form onSubmit={onSubmit}><div className="name-fields"><label>Ad<input required maxLength="80" name="firstName" /></label><label>Soyad<input required maxLength="80" name="lastName" /></label></div><label>E-posta<input required name="email" type="email" placeholder="uye@firma.com" /></label><label>Rol<select name="role" defaultValue="TeamMember"><option value="TeamMember">Ekip üyesi</option><option value="ProjectManager">Proje yöneticisi</option></select></label><label>Geçici parola<input required minLength="8" name="password" type="password" placeholder="En az 8 karakter" /></label><label>Parola tekrarı<input required minLength="8" name="confirmPassword" type="password" /></label><button disabled={loading}>{loading ? 'Oluşturuluyor…' : 'Kullanıcıyı oluştur'}</button></form></section></div>;
}

function MemberModal({ project, members, eligibleUsers, onClose, onSubmit, onRemove, loading }) {
  return <div className="modal-backdrop"><section className="modal team-modal"><div className="modal-header"><div><h2>{project.name}</h2><p className="muted">Proje ekibini doğrudan yönet.</p></div><button className="close-button" type="button" onClick={onClose}>×</button></div><form className="member-add-form" onSubmit={onSubmit}><label>Aktif kullanıcı<select required name="userId" defaultValue=""><option value="" disabled>{eligibleUsers.length ? 'Kullanıcı seç' : 'Eklenebilecek kullanıcı yok'}</option>{eligibleUsers.map(user => <option key={user.id} value={user.id}>{user.firstName} {user.lastName} · {user.email}</option>)}</select></label><button disabled={loading || !eligibleUsers.length}>Projeye ekle</button></form><div className="member-list"><h3>Mevcut üyeler ({members.filter(member => member.isActive).length})</h3>{members.filter(member => member.isActive).map(member => <article key={member.userId}><span><b>{member.firstName} {member.lastName}</b><small>{member.email} · {Number(member.memberRole) === 1 ? 'Proje yöneticisi' : 'Ekip üyesi'}</small></span>{Number(member.memberRole) !== 1 && <button className="danger-button" type="button" disabled={loading} onClick={() => onRemove(member)}>Çıkar</button>}</article>)}</div></section></div>;
}

function ProjectModal({ isAdmin, managers, onClose, onSubmit, loading }) {
  const today = new Date().toISOString().slice(0, 10);
  return <div className="modal-backdrop"><section className="modal"><div className="modal-header"><h2>Yeni proje</h2><button className="close-button" type="button" onClick={onClose}>×</button></div><form onSubmit={onSubmit}><label>Proje adı<input required minLength="3" name="name" placeholder="Örn. Mobil uygulama" /></label>{isAdmin && <label>Proje yöneticisi<select required name="managerUserId" defaultValue=""><option value="" disabled>Ekipten kullanıcı seç</option>{managers.map(user => <option key={user.id} value={user.id}>{user.firstName} {user.lastName} · {user.email}</option>)}</select></label>}<label>Açıklama<textarea name="description" rows="3" placeholder="Projenin amacı ve kapsamı" /></label><div className="name-fields"><label>Başlangıç<input required name="startDate" defaultValue={today} type="date" /></label><label>Hedef bitiş<input required name="targetEndDate" type="date" /></label></div><button disabled={loading}>{loading ? 'Oluşturuluyor…' : 'Projeyi oluştur'}</button></form></section></div>;
}

function AdminPanel({ overview, loading }) {
  return <>
    <section className="panel"><div className="panel-heading"><div><h2>Ekip üyeleri</h2><p>Kayıtlı ve aktif kullanıcılar</p></div><b>{overview.users.length}</b></div>{loading ? <p className="muted">Yükleniyor…</p> : overview.users.length ? <div className="workload-table"><div className="workload-head"><span>Kullanıcı</span><span>Rol</span><span>Durum</span></div>{overview.users.map(user => <div className="workload-row" key={user.id}><span><b>{user.firstName} {user.lastName}</b><small>{user.email}</small></span><span>{user.roles?.join(', ') || 'Ekip üyesi'}</span><span>Aktif</span></div>)}</div> : <p className="muted">Henüz ekip üyesi yok.</p>}</section>
    <section className="panel"><div className="panel-heading"><div><h2>Projeler</h2><p>Sistemdeki tüm aktif projeler</p></div><b>{overview.projects.length}</b></div>{overview.projects.length ? <div className="workload-table"><div className="workload-head"><span>Proje</span><span>Tarih</span><span>Durum</span></div>{overview.projects.map(project => <div className="workload-row" key={project.id}><span><b>{project.name}</b><small>{project.description || 'Açıklama yok'}</small></span><span>{project.startDate} — {project.targetEndDate}</span><span>{statusText(project.status)}</span></div>)}</div> : <p className="muted">Henüz proje oluşturulmadı.</p>}</section>
  </>;
}

function TaskModal({ projects, members, adminAutoMembership, onProjectChange, onClose, onSubmit, loading }) {
  const today = new Date().toISOString().slice(0, 10);
  return <div className="modal-backdrop"><section className="modal task-create-modal"><div className="modal-header"><h2>Yeni görev</h2><button className="close-button" type="button" onClick={onClose}>×</button></div>{projects.length ? <><form id="create-task-form" className="modal-scroll-form" onSubmit={onSubmit}><label>Proje<select required name="projectId" defaultValue={projects[0].id} onChange={event => onProjectChange(event.target.value)}>{projects.map(project => <option key={project.id} value={project.id}>{project.name}</option>)}</select></label><label>Görevi ata<select required name="assignedUserId" disabled={!members.length}><option value="">{members.length ? 'Aktif kullanıcı seç' : 'Aktif kullanıcı bulunamadı'}</option>{members.filter(member => member.isActive !== false).map(member => <option key={member.userId ?? member.id} value={member.userId ?? member.id}>{member.firstName} {member.lastName} · {member.email}</option>)}</select></label><label>Görev başlığı<input required minLength="3" name="title" placeholder="Örn. Giriş ekranını tamamla" /></label><label>Açıklama<textarea name="description" rows="3" /></label><div className="name-fields"><label>Öncelik<select name="priority" defaultValue="2"><option value="1">Düşük</option><option value="2">Orta</option><option value="3">Yüksek</option><option value="4">Kritik</option></select></label><label>Başlangıç<input name="startDate" defaultValue={today} type="date" /></label></div><label>Son tarih<input required name="dueDate" type="date" /></label><p className="muted">{adminAutoMembership ? 'Seçilen aktif kullanıcı projede değilse otomatik olarak projeye üye eklenir.' : 'Yalnızca onaylanmış aktif proje üyeleri seçilebilir.'}</p></form><div className="modal-actions"><button type="submit" form="create-task-form" disabled={loading || !members.length}>{loading ? 'Oluşturuluyor…' : 'Görevi oluştur ve ata'}</button></div></> : <p className="muted">Önce bir proje oluşturmalısın.</p>}</section></div>;
}

function EditTaskModal({ task, members, onClose, onSubmit, loading }) {
  return <div className="modal-backdrop"><section className="modal"><div className="modal-header"><h2>Görevi güncelle</h2><button className="close-button" type="button" onClick={onClose}>×</button></div><form onSubmit={onSubmit}><label>Görevi ata<select required name="assignedUserId" defaultValue={task.assignedUserId}>{members.filter(member => member.isActive).map(member => <option key={member.userId} value={member.userId}>{member.firstName} {member.lastName} · {member.email}</option>)}</select></label><label>Görev başlığı<input required minLength="3" maxLength="200" name="title" defaultValue={task.title} /></label><label>Açıklama<textarea name="description" maxLength="5000" rows="3" defaultValue={task.description ?? ''} /></label><div className="name-fields"><label>Öncelik<select name="priority" defaultValue={task.priority}><option value="1">Düşük</option><option value="2">Orta</option><option value="3">Yüksek</option><option value="4">Kritik</option></select></label><label>Başlangıç<input name="startDate" type="date" defaultValue={task.startDate ?? ''} /></label></div><label>Son tarih<input required name="dueDate" type="date" defaultValue={task.dueDate} /></label><button disabled={loading}>{loading ? 'Kaydediliyor…' : 'Değişiklikleri kaydet'}</button></form></section></div>;
}

function TaskDetailModal({ task, comments, attachments, onClose, onSubmit, onUpload }) {
  return <div className="modal-backdrop"><section className="modal team-modal"><div className="modal-header"><div><h2>{task.title}</h2><p className="muted">Görev yorumları ve dosyalar</p></div><button className="close-button" type="button" onClick={onClose}>×</button></div><section className="task-description-box"><span>Yöneticinin görev açıklaması</span><p>{task.description || 'Bu görev için açıklama eklenmemiş.'}</p></section><div className="comment-list">{comments.length ? comments.map(comment => <article className="comment" key={comment.id}><header className="comment-author"><span className="comment-avatar">{comment.userFullName?.split(' ').map(part => part[0]).slice(0, 2).join('').toUpperCase() || '?'}</span><span><b>{comment.userFullName || comment.userEmail || 'Bilgisi alınamayan kullanıcı'}</b><small>{comment.userEmail || 'E-posta bilgisi alınamadı'}</small></span><em className={`comment-role role-${comment.userRole?.toLowerCase()}`}>{commentRoleText(comment.userRole)}</em></header><p>{comment.content}</p><time>{new Date(comment.createdDate).toLocaleString('tr-TR')}{comment.updatedDate ? ' · düzenlendi' : ''}</time></article>) : <p className="muted">Henüz yorum yok.</p>}</div><form onSubmit={onSubmit}><label>Yeni yorum<textarea required name="content" maxLength="1500" rows="3" placeholder="Görevle ilgili güncellemeni yaz…" /></label><p className="form-hint">Yorumu görev sahibi dışında biri yazarsa görev sahibine bildirim gönderilir. Kendi yorumun için kendine bildirim oluşturulmaz.</p><button>Yorum ekle</button></form><form className="upload-form" onSubmit={onUpload}><label>Dosya ekle<input name="file" type="file" accept=".pdf,.png,.jpg,.jpeg,.docx,.xlsx,.txt" /></label><button>Dosyayı yükle</button></form>{attachments.length > 0 && <div className="attachment-list">{attachments.map(file => <span key={file.id}>📎 {file.originalFileName} ({Math.ceil(file.fileSize / 1024)} KB)</span>)}</div>}</section></div>;
}

function ListPanel({ page, items, loading, canJoin, canManageMembers, canEditTasks, onStatusChange, onNotificationOpen, onTaskDetail, onTaskEdit, onManageMembers, onJoin, onReview }) {
  if (loading) return <section className="panel"><p className="muted">Veriler yükleniyor…</p></section>;
  if (!items.length) return <section className="panel"><p className="muted">Henüz görüntülenecek kayıt bulunmuyor.</p></section>;
  return <section className="panel list-panel">{items.map(item => {
    if (page === 'notifications') return <article className={`list-row notification ${item.isRead ? '' : 'unread'}`} key={item.id} onClick={() => onNotificationOpen(item)}><div><strong>{item.title}</strong><span>{item.message} · {new Date(item.createdDate).toLocaleString('tr-TR')}</span></div><div className="notification-actions"><b className="badge">{item.isRead ? 'Okundu' : 'Yeni'}</b>{item.relatedEntityId && <small>İlgili kaydı aç →</small>}</div></article>;
    const detail = page === 'myRequests' ? `${item.projectName} · ${statusText(item.status)}` : page === 'requests' ? `${item.email} · ${new Date(item.createdDate).toLocaleString('tr-TR')}` : item.dueDate ? `Son tarih: ${item.dueDate} · Öncelik: ${priorityText(item.priority)}` : `${item.startDate} — ${item.targetEndDate}`;
    return <article className="list-row" key={`${item.membershipView ?? page}-${item.id}`}><div><strong>{page === 'requests' ? `${item.firstName} ${item.lastName} · ${item.projectName}` : item.title ?? item.name ?? item.projectName}</strong>{page === 'tasks' && <p className="task-description-preview">{item.description || 'Açıklama eklenmemiş.'}</p>}<span>{detail}</span></div>
      {page === 'tasks' ? <div className="row-actions"><select className="status-select" value={item.status} onChange={event => onStatusChange(item.id, event.target.value)}><option value="1">Yeni</option><option value="2">Devam ediyor</option><option value="3">Beklemede</option><option value="4">Tamamlandı</option><option value="5">İptal edildi</option></select>{canEditTasks && <button className="mini-button" onClick={() => onTaskEdit(item)}>Düzenle</button>}<button className="mini-button" onClick={() => onTaskDetail(item)}>Yorumlar</button></div>
      : page === 'projects' && item.membershipView === 'member' ? <div className="row-actions"><b className="badge completed">Kayıtlısın</b><b className="badge">{statusText(item.status)}</b></div>
      : page === 'projects' && canJoin ? <button className="mini-button" onClick={() => onJoin(item.id)}>Katılım isteği gönder</button>
      : page === 'projects' && canManageMembers ? <div className="row-actions"><b className="badge">{statusText(item.status)}</b><button className="mini-button" onClick={() => onManageMembers(item)}>Üyeleri yönet</button></div>
      : page === 'requests' ? <div className="row-actions"><button className="mini-button" onClick={() => onReview(item.id, true)}>Onayla</button><button className="mini-button" onClick={() => onReview(item.id, false)}>Reddet</button></div>
      : <b className="badge">{statusText(item.status)}</b>}
    </article>;
  })}</section>;
}

const statusText = (status) => ({ 1: 'Yeni', 2: 'Devam ediyor', 3: 'Beklemede', 4: 'Tamamlandı', 5: 'İptal edildi' }[status] ?? status);
const priorityText = (priority) => ({ 1: 'Düşük', 2: 'Orta', 3: 'Yüksek', 4: 'Acil' }[priority] ?? priority);
const commentRoleText = (role) => ({ Admin: 'Yönetici', ProjectManager: 'Proje yöneticisi', TeamMember: 'Ekip üyesi' }[role] ?? 'Ekip üyesi');

function LoginForm({ isRegistering, onSubmit, onSwitch, error, loading }) {
  return <main className="login-page"><div className="auth-layout"><section className="login-card"><div className="logo">✓</div><p className="eyebrow">EKİP GÖREV YÖNETİMİ</p><h1>{isRegistering ? 'Hesap oluştur' : 'Tekrar hoş geldin'}</h1><p className="muted">{isRegistering ? 'Hesabını hemen oluştur; ardından istediğin projeye katılım isteği gönder.' : 'Çalışma alanına devam etmek için giriş yap.'}</p><form onSubmit={onSubmit}>{isRegistering && <div className="name-fields"><label>Ad<input required name="firstName" /></label><label>Soyad<input required name="lastName" /></label></div>}<label>E-posta<input required name="email" type="email" placeholder="ornek@firma.com" /></label><label>Parola<input required minLength="8" name="password" type="password" placeholder="En az 8 karakter" /></label>{isRegistering && <label>Parola tekrarı<input required name="confirmPassword" type="password" /></label>}{error && <div className="alert">{error}</div>}<button disabled={loading}>{loading ? 'İşleniyor…' : isRegistering ? 'Kayıt ol' : 'Giriş yap'}</button></form><p className="switch-text">{isRegistering ? 'Zaten hesabın var mı?' : 'Henüz hesabın yok mu?'} <button type="button" onClick={onSwitch}>{isRegistering ? 'Giriş yap' : 'Kayıt ol'}</button></p></section><aside className="auth-visual"><div className="bug-swarm" aria-hidden="true">{Array.from({ length: 20 }, (_, index) => <span className="auth-bug" style={{ '--x': `${(index * 37) % 92}%`, '--y': `${(index * 53) % 88}%`, '--size': `${15 + (index % 3) * 3}px`, '--duration': `${9 + (index % 5) * 1.7}s`, '--delay': `${index * -0.7}s` }} key={index}>🐞</span>)}</div><p className="eyebrow">BİRLİKTE DAHA DÜZENLİ</p><h2>İşlerin, ekibin ve projelerin tek bir yerde.</h2><p>Görevleri takip et, projene katıl ve ekip akışından kopma.</p><div className="visual-pills"><span>Projeler</span><span>Görevler</span><span>Takip</span></div></aside></div></main>;
}
