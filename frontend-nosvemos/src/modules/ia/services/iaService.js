import api from "../../../core/api/api";

export const analizar = async (file) => {
  const formData = new FormData();
  formData.append("frame", file);

  const res = await api.post("/ia/analizar-camara", formData);
  return res.data;
};