import api from "../../../core/api/api";

export const login = async (email, password) => {
  const res = await api.post("/autenticacion/login", {
    email,
    password,
  });

  localStorage.setItem("token", res.data.access_token);
};