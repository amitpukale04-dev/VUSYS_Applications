export const PUBLIC_AUTH_ROUTES=["/login"];
export const isPublicAuthRoutePath=(path="")=>PUBLIC_AUTH_ROUTES.some(x=>path===x||path.startsWith(x+"/"));
