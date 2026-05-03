import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";

export default function Login({ onLogin }) {
  const [creds, setCreds] = useState({});
  const [error, setError] = useState("");
  const navigate = useNavigate();
  const handleLogin = async () => {
    try {
      const response = await fetch("https://nknd9k-8080.csb.app/api/login", {
        method: "post",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify(creds),
      });
      if (response.ok) {
        onLogin && onLogin({ username: creds.username });
        navigate("/stats");
      } else setError("Invalid username or password!");
    } catch (error) {
      console.error("Login error:", error);
      setError("Login failed!");
    }
  };
  return (
    <div style={{ padding: 10 }}>
      {" "}
      <br />
      <span>Username:</span>
      <br />
      <input
        type="text"
        onChange={(e) => setCreds({ ...creds, username: e.target.value })}
      />
      <br />
      <span>Password:</span>
      <br />
      <input
        type="password"
        onChange={(e) => setCreds({ ...creds, password: e.target.value })}
      />
      <br />
      <br />
      <button onClick={handleLogin}>Login</button>
      <p>{error}</p>
    </div>
  );
}

export function Register() {
  const [res, setRes] = useState("");
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm();

  const onsubmit = async (data) => {
    try {
      const response = await fetch("https://nknd9k-8080.csb.app/api/register", {
        method: "post",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify(data),
      });
      if (response.ok) {
        setRes("register successfully");
        navigate("/login");
      } else setRes("Invalid username or password!");
    } catch (error) {
      console.error("register error:", error);
      setRes("register failed!");
    }
  };
  return (
    <>
      <form onSubmit={handleSubmit(onsubmit)}>
        <div style={{ padding: 10 }}>
          <br />
          <span>Username:</span>
          <br />
          <input type="text" {...register("username", { required: true })} />
          <br />
          <span>Password:</span>
          <br />
          <input type="text" {...register("password", { required: true })} />
          <br />
          <button type="submit">register</button>
          {res && <p>{res}</p>}
        </div>
      </form>
    </>
  );
}
