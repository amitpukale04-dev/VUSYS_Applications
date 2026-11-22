import {Routes,Route,Navigate} from "react-router-dom";
import ProtectedRoute from "./ProtectedRoute";import AppLayout from "../components/layout/AppLayout";
import Login from "../pages/auth/Login";import HomePage from "../pages/dashboard/HomePage";
import RepositoriesPage from "../pages/repositories/RepositoriesPage";import PullRequestsPage from "../pages/pullRequests/PullRequestsPage";
import ReviewWorkspacePage from "../pages/reviews/ReviewWorkspacePage";import ReviewHistoryPage from "../pages/reviews/ReviewHistoryPage";
import ConfigurationPage from "../pages/configuration/ConfigurationPage";
import NotFound from "../pages/errors/NotFound";import ErrorPage from "../pages/errors/ErrorPage";
import {useAuth} from "../context/AuthContext";
export default function AppRoutes(){const {isAuthenticated}=useAuth();return <Routes>
<Route path="/login" element={isAuthenticated?<Navigate to="/" replace/>:<Login/>}/>
<Route element={<ProtectedRoute/>}><Route element={<AppLayout/>}>
<Route index element={<HomePage/>}/><Route path="/repositories" element={<RepositoriesPage/>}/>
<Route path="/pull-requests" element={<PullRequestsPage/>}/><Route path="/reviews/new" element={<ReviewWorkspacePage/>}/>
<Route path="/reviews/history" element={<ReviewHistoryPage/>}/><Route path="/configuration" element={<ConfigurationPage/>}/>
<Route path="/user-profile" element={<ConfigurationPage profileOnly/>}/><Route path="/error" element={<ErrorPage/>}/>
</Route></Route><Route path="*" element={<NotFound/>}/></Routes>}
