import { useMsal } from "@azure/msal-react";

function MsalTest () {
    const { accounts } = useMsal();

    return (
        <div>
          <h2>Welcome, { accounts[0].name }</h2>
          <pre style={ { display: "none" }}>{ JSON.stringify(accounts, null, 2) }</pre>
        </div>
    )
}

export default MsalTest