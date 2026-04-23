import Sidebar from "../sidebar/Sidebar";
import Navbar from "../navbar/Navbar";

export default function Layout({ children }) {
  return (
    <div className="flex h-screen bg-gray-100">
      <Sidebar />

      <div className="flex-1 flex flex-col">
        <Navbar />

        <main className="p-6 overflow-auto">
          {children}
        </main>
      </div>
    </div>
  );
}