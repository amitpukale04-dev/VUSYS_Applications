import axios from "axios";
const apiClient=axios.create({baseURL:import.meta.env.VITE_API_BASE_URL||"/api",timeout:100000,headers:{"Content-Type":"application/json"}});
apiClient.interceptors.request.use(config=>{const token=JSON.parse(sessionStorage.getItem("vusys_code_review_session")||localStorage.getItem("vusys_code_review_session")||"null")?.token;if(token)config.headers.Authorization=`Bearer ${token}`;return config;});
export default apiClient;
