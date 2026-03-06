import { useState, useEffect } from 'react';

function App() {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [todos, setTodos] = useState([]);
  const [newTitle, setNewTitle] = useState('');
  const [error, setError] = useState(null);

  // Check auth status
  useEffect(() => {
    fetch('/auth/user')
      .then(res => res.json())
      .then(data => {
        setUser(data);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, []);

  // Fetch todos when authenticated
  useEffect(() => {
    if (user?.isAuthenticated) {
      fetchTodos();
    }
  }, [user]);

  async function fetchTodos() {
    try {
      const res = await fetch('/api/todos');
      if (!res.ok) throw new Error('Failed to load todos');
      const data = await res.json();
      setTodos(data);
      setError(null);
    } catch (err) {
      setError(err.message);
    }
  }

  async function addTodo(e) {
    e.preventDefault();
    if (!newTitle.trim()) return;
    try {
      const res = await fetch('/api/todos', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: newTitle.trim(), isComplete: false }),
      });
      if (!res.ok) throw new Error('Failed to add todo');
      setNewTitle('');
      await fetchTodos();
    } catch (err) {
      setError(err.message);
    }
  }

  async function toggleTodo(todo) {
    try {
      const res = await fetch(`/api/todos/${todo.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...todo, isComplete: !todo.isComplete }),
      });
      if (!res.ok) throw new Error('Failed to update todo');
      await fetchTodos();
    } catch (err) {
      setError(err.message);
    }
  }

  async function deleteTodo(id) {
    try {
      const res = await fetch(`/api/todos/${id}`, { method: 'DELETE' });
      if (!res.ok) throw new Error('Failed to delete todo');
      await fetchTodos();
    } catch (err) {
      setError(err.message);
    }
  }

  if (loading) {
    return <div className="app"><div className="loading">Loading...</div></div>;
  }

  if (!user?.isAuthenticated) {
    return (
      <div className="app">
        <header>
          <h1>QA Task - Todo App</h1>
          <p>Please log in to manage your todos.</p>
        </header>
        <div className="login-prompt">
          <a href="/login">Log In with Keycloak</a>
        </div>
      </div>
    );
  }

  return (
    <div className="app">
      <header>
        <h1>QA Task - Todo App</h1>
        <p className="user-info">
          Logged in as <strong data-testid="user-name">{user.name}</strong>
          {' | '}
          <a href="/logout" className="logout-link">Log out</a>
        </p>
      </header>

      {error && <div className="error" data-testid="error-message">{error}</div>}

      <form className="todo-form" onSubmit={addTodo}>
        <input
          type="text"
          value={newTitle}
          onChange={e => setNewTitle(e.target.value)}
          placeholder="Add a new todo..."
          data-testid="todo-input"
        />
        <button type="submit" data-testid="add-button">Add</button>
      </form>

      {todos.length === 0 ? (
        <div className="empty-state" data-testid="empty-state">No todos yet. Add one above!</div>
      ) : (
        <ul className="todo-list" data-testid="todo-list">
          {todos.map(todo => (
            <li key={todo.id} className={`todo-item ${todo.isComplete ? 'complete' : ''}`} data-testid="todo-item">
              <input
                type="checkbox"
                checked={todo.isComplete}
                onChange={() => toggleTodo(todo)}
                data-testid={`todo-checkbox-${todo.id}`}
              />
              <span data-testid={`todo-title-${todo.id}`}>{todo.title}</span>
              <button onClick={() => deleteTodo(todo.id)} data-testid={`todo-delete-${todo.id}`}>
                Delete
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default App;
