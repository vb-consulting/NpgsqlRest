interface IAuthGetUserDetailsRequest {
    userId: string | null;
}

interface IAuthGetUserDetailsResponse {
    id: number | null;
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    created: string | null;
    group: string | null;
    permissions: string[] | null;
    company: string | null;
}

interface IAuthLoginRequest {
    username: string | null;
    password: string | null;
}

const _baseUrl = "http://localhost:5001";

const _parseQuery = (query: Record<any, any>) => "?" + Object.keys(query)
    .map(key => {
        const value = query[key] ? query[key] : "";
        if (Array.isArray(value)) {
            return value.map(s => s ? `${key}=${encodeURIComponent(s)}` : `${key}=`).join("&");
        }
        return `${key}=${encodeURIComponent(value)}`;
    })
    .join("&");

/**
* function auth.get_user_details(
*     _user_id text
* )
* returns table(
*     id integer,
*     username text,
*     first_name text,
*     last_name text,
*     created timestamp without time zone,
*     "group" text,
*     permissions text[],
*     company text
* )
* 
* @remarks
* comment on function auth.get_user_details is 'HTTP GET
* 
* @see FUNCTION auth.get_user_details
*/
export async function authGetUserDetails(request: IAuthGetUserDetailsRequest) : Promise<IAuthGetUserDetailsResponse[]> {
    const response = await fetch(_baseUrl + "/api/auth/get-user-details" + _parseQuery(request), {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return await response.json() as IAuthGetUserDetailsResponse[];
}

/**
* function auth.login(
*     _username text,
*     _password text
* )
* returns table(
*     status integer,
*     name_identifier text,
*     name text,
*     role text[]
* )
* 
* @remarks
* comment on function auth.login is 'HTTP POST
* Login';
* 
* @see FUNCTION auth.login
*/
export async function authLogin(request: IAuthLoginRequest) : Promise<string> {
    const response = await fetch(_baseUrl + "/api/auth/login", {
        method: "POST",
        body: JSON.stringify(request)
    });
    return await response.text();
}

/**
* function auth.logout()
* returns void
* 
* @remarks
* comment on function auth.logout is 'HTTP POST
* Logout';
* 
* @see FUNCTION auth.logout
*/
export async function authLogout() : Promise<void> {
    await fetch(_baseUrl + "/api/auth/logout", {
        method: "POST",
    });
}

