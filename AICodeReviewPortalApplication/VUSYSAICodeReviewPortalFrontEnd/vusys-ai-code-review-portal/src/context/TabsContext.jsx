import {createContext,useContext,useEffect,useMemo,useState} from "react";
import {useLocation,useNavigate} from "react-router-dom";
import {CODE_REVIEW_MENUS} from "../constants/CodeReviewMenus";
const C=createContext(null);const HOME={path:"/",label:"Home",svgIcon:"home",tone:"indigo",closable:false};
const all=CODE_REVIEW_MENUS.flatMap(x=>x.children||[x]);
export function TabsProvider({children}){const loc=useLocation();const navigate=useNavigate();const [tabs,setTabs]=useState([HOME]);
 useEffect(()=>{if(loc.pathname==="/"||loc.pathname==="/login")return;const m=all.find(x=>x.path===loc.pathname);if(!m)return;setTabs(prev=>prev.some(x=>x.path===m.path)?prev:[...prev,{...m,closable:true}]);},[loc.pathname]);
 const closeTab=(path)=>{setTabs(prev=>prev.filter(x=>x.path!==path));if(loc.pathname===path)navigate("/");};
 const value=useMemo(()=>({tabs,closeTab}),[tabs]);return <C.Provider value={value}>{children}</C.Provider>;
}
export const useTabs=()=>useContext(C);
