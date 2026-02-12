import { Outlet } from "react-router-dom";

export default function Layout() {
    return (
        <div className="flex flex-col gap-4 w-full h-full">
            <Outlet />
        </div>
    );
}
