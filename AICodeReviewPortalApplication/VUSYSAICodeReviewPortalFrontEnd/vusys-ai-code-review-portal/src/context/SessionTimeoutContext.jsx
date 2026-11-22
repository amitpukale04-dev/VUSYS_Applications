import {createContext,useCallback,useContext,useEffect,useMemo,useRef,useState} from "react";
import {useNavigate} from "react-router-dom";import {useAuth} from "./AuthContext";
const C=createContext(null);const TIMEOUT=30*60*1000;
export function SessionTimeoutProvider({children}){const {isAuthenticated,logout}=useAuth();const nav=useNavigate();const timer=useRef(null);const [expired,setExpired]=useState(false);
 const expire=useCallback(()=>{if(isAuthenticated)setExpired(true);},[isAuthenticated]);
 useEffect(()=>{if(!isAuthenticated){setExpired(false);return;}const reset=()=>{clearTimeout(timer.current);timer.current=setTimeout(expire,TIMEOUT);};const events=["mousemove","keydown","click","touchstart","scroll"];events.forEach(e=>window.addEventListener(e,reset,{passive:true}));window.addEventListener("vusys:session-expired",expire);reset();return()=>{clearTimeout(timer.current);events.forEach(e=>window.removeEventListener(e,reset));window.removeEventListener("vusys:session-expired",expire);};},[isAuthenticated,expire]);
 const signInAgain=useCallback(async()=>{await logout();setExpired(false);nav("/login",{replace:true});},[logout,nav]);
 const value=useMemo(()=>({expired,signInAgain}),[expired,signInAgain]);return <C.Provider value={value}>{children}</C.Provider>;
}
export const useSessionTimeout=()=>useContext(C);
