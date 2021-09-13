import { useMsal } from "@azure/msal-react";

function MsalTest () {
    const { accounts } = useMsal();

    return (
        <div>
          <h2>Accounts</h2>
          <pre>{ JSON.stringify(accounts, null, 2) }</pre>
        </div>
    )
}

export default MsalTest