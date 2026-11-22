import { createContext, useCallback, useContext, useMemo, useState } from "react";
import { loginUser } from "../services/authService";
import { CODE_REVIEW_MENUS } from "../constants/CodeReviewMenus";
import { STORAGE_KEYS } from "../utils/storageKeys";
const AuthContext = createContext(null);
const SESSION_KEY = "vusys_code_review_session";
const readSession = () => { try { const s=JSON.parse(sessionStorage.getItem(SESSION_KEY)||localStorage.getItem(SESSION_KEY)||"null"); return s?.expiresAt && Date.parse(s.expiresAt)>Date.now()?s:null; } catch {return null;} };
export function AuthProvider({ children }) {
  const [session,setSession]=useState(readSession);
  const [rememberedPassword, setRememberedPassword] = useState("");
  const login=useCallback(async ({ email, password, remember }) => {
    const result=await loginUser({ username:email, password });
    const next={ token:result.token, expiresAt:result.expiresUtc ?? result.expiresAt, user:{name:result.username||email,email,role:"Admin",id:1,LastLogoutDate:new Date().toLocaleString()} };
    sessionStorage.setItem(SESSION_KEY,JSON.stringify(next));
    if(remember)localStorage.setItem(SESSION_KEY,JSON.stringify(next)); else localStorage.removeItem(SESSION_KEY);
    setRememberedPassword(remember ? password : "");
    setSession(next);
    return {requiresPasswordReset:false,requiresTwoFactor:false};
  },[]);
  const logout=useCallback(async()=>{sessionStorage.removeItem(SESSION_KEY);localStorage.removeItem(SESSION_KEY);localStorage.removeItem(STORAGE_KEYS.rememberedPassword);setSession(null);},[]);
  const value=useMemo(()=>({user:session?.user||null,token:session?.token||null,isAuthenticated:Boolean(session?.token),authStage:session?.token?"authenticated":"",rememberedPassword,setRememberedPassword,login,logout,menuItems:CODE_REVIEW_MENUS,menuAccessLoading:false,actionAccessItems:[],userSecurityAccessItems:[],userSecurityAccessLoading:false}),[session,rememberedPassword,login,logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
export function useAuth(){const c=useContext(AuthContext);if(!c)throw new Error("useAuth requires AuthProvider");return c;}
