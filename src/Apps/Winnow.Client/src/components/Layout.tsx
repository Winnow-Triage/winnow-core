import { Outlet } from "react-router-dom";

export default function Layout() {
  return (
    <div className="flex flex-col gap-4 w-full bg-background p-8 flex-1 min-h-0">
      <Outlet />
    </div>
  );
}
