import api from "../../../core/api/api";

export const getUsuarios = async () => {
  const res = await api.get("/usuarios");
  return res.data;
};