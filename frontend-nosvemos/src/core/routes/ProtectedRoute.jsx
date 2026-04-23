import { Navigate } from "react-router-dom";
import { isAuthenticated } from "../auth/authStore";

export default function ProtectedRoute({ children }) {
  if (!isAuthenticated()) return <Navigate to="/" />;
  return children;
}