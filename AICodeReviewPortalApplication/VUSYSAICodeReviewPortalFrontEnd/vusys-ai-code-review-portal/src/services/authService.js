import apiClient from "./apiClient";
export async function loginUser({username,password}){const {data}=await apiClient.post("/auth/login",{username,password},{skipGlobalErrorToast:true});return data;}
export async function logoutUser(){return true;}
