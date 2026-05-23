import { useState, useEffect } from "react";
import { useForm } from "react-hook-form";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Link,
  Outlet,
  useParams,
  Navigate,
  useNavigate,
} from "react-router-dom";

import Stats from "./Stats";
import PostLists from "./PostLists";
import Post, { NewPost, UpdatePost } from "./Post";
import ProtectedRoute from "./ProtectedRoute";
import Login, { Register } from "./Auth";
import Users from "./Users";

function Home() {
  return (
    <div style={{ padding: 20 }}>
      <h2>Home View</h2>
    </div>
  );
}

function About() {
  return (
    <div style={{ padding: 20 }}>
      <h2>About View</h2>
    </div>
  );
}

function NoMatch() {
  return (
    <div style={{ padding: 20 }}>
      <h2>404: Page Not Found</h2>
    </div>
  );
}

function Posts() {
  return (
    <div style={{ padding: 20 }}>
      <h2>Blog</h2>
      <hr />
      <Outlet />
    </div>
  );
}

function AppLayout() {
  const [user, setUser] = useState(null);
  const navigate = useNavigate();

  const logOut = () => {
    setUser(null);
    navigate("/");
  };
  return (
    <>
      <nav
        style={{
          margin: 10,
          borderBottom: "1px solid #ccc",
          paddingBottom: 10,
        }}
      >
        <Link to="/" style={{ padding: 5 }}>
          Home
        </Link>
        <Link to="/posts" style={{ padding: 5 }}>
          Posts
        </Link>
        <Link to="/about" style={{ padding: 5 }}>
          About
        </Link>
        <span> | </span>
        {user && (
          <Link to="/stats" style={{ padding: 5 }}>
            Stats
          </Link>
        )}
        {user && (
          <Link to="/newpost" style={{ padding: 5 }}>
            New Post
          </Link>
        )}
        {!user && (
          <Link to="/login" style={{ padding: 5 }}>
            Login
          </Link>
        )}
        {!user && (
          <Link to="/register" style={{ padding: 5 }}>
            Register
          </Link>
        )}
        {user && (
          <span onClick={logOut} style={{ padding: 5, cursor: "pointer" }}>
            Logout
          </span>
        )}
        {user && (
          <Link to="/users" style={{ padding: 5 }}>
            Users
          </Link>
        )}
      </nav>

      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/posts" element={<Posts />}>
          <Route index element={<PostLists />} />
          <Route path=":slug" element={<Post />} />
        </Route>
        <Route path="/about" element={<About />} />
        <Route path="/login" element={<Login onLogin={setUser} />} />
        <Route path="/register" element={<Register />} />
        <Route
          path="/stats"
          element={
            <ProtectedRoute user={user}>
              <Stats />
            </ProtectedRoute>
          }
        />
        <Route
          path="/newpost"
          element={
            <ProtectedRoute user={user}>
              <NewPost />
            </ProtectedRoute>
          }
        />
        <Route path="/users" element={<Users />} />
        <Route path="*" element={<NoMatch />} />
      </Routes>
    </>
  );
}

function App() {
  return (
    <Router>
      <AppLayout />
    </Router>
  );
}

export default App;
