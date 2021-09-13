import { apiConfig } from "./apiConfig"

export const callB2CApi = (endpoint, msalinstance, msalaccount, options) => {
    return new Promise( (resolve, reject) => {
        msalinstance.acquireTokenSilent({
            scopes: apiConfig.scopes,
            account: msalaccount
        }).then((tokenResponse) => {
            console.log('tokenResponse')
            console.log(tokenResponse)
            const qs = (endpoint.indexOf('?') > 0) ? `&code=${apiConfig.code}` : `?code=${apiConfig.code}`
            const fetchOptions = (options) ? options : {}
            if (!fetchOptions.headers) {
                fetchOptions["headers"] = {
                }
            }
            fetchOptions.headers['authorization'] = `Bearer ${tokenResponse.accessToken}`
            fetch(`${apiConfig.baseurl}/${endpoint}${qs}`, fetchOptions)
                .then((response) => resolve(response.json()))
               
        })
    })
}