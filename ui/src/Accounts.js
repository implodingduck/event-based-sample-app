import React, {Component, useState, useEffect} from 'react';
import logo from './logo.svg';
import './App.css';
import Account from './Account'
import { useMsal, useAccount } from "@azure/msal-react";

import { Button, Container, Modal, Col, Row } from 'react-bootstrap'
import { apiConfig } from './apiConfig'

function Accounts() {
    const { instance, accounts, inProgress } = useMsal();
    const msalaccount = useAccount(accounts[0] || {});
    const [myaccounts, setMyAccounts] = useState([]);
    const [showCreateAccount, setShowCreateAccount] = useState(false);
    const [createAccount, setCreateAccount] = useState({
        "type": "Checking",
        "balance": 1000
    });

    const refreshAccounts = () => {
        instance.acquireTokenSilent({
            scopes: apiConfig.scopes,
            account: msalaccount
        }).then((tokenResponse) => {
            console.log('Token Response: ')
            console.log(tokenResponse)
            fetch(`${apiConfig.baseurl}/api/accounts/?code=${apiConfig.code}`, {
                headers: {
                    "authorization": `Bearer ${tokenResponse.accessToken}`
                }
            })
                .then((response) => response.json())
                .then(accountsJson => {
                    console.log(accountsJson)
                    setMyAccounts(accountsJson);
                });
        })
    }

    useEffect(() => {
        refreshAccounts()
    },[])

    const toggleCreateAccount = () => {
        setShowCreateAccount(!showCreateAccount)
    }

    const handleCreateAccount = () => {
        setShowCreateAccount(false)
        fetch('/api/accounts/', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                "type": createAccount.type,
                "balance": createAccount.balance
            })
        } ).then( () => window.location.reload())
    }

    const handleTypeChange = (e) => {
        const updatedNewAccount = JSON.parse(JSON.stringify(createAccount))
        updatedNewAccount.type = e.target.value
        setCreateAccount(updatedNewAccount)
    }
    
    const handleBalanceChange = (e) => {
        const updatedNewAccount = JSON.parse(JSON.stringify(createAccount));
        const parsed = parseInt(e.target.value);
        updatedNewAccount.balance = (isNaN(parsed) || parsed < 0) ? 0 : parsed
        setCreateAccount(updatedNewAccount)
    }

    return (
        <Container className="topspacer">
            <Row>
                <Col>
                    <Button variant="primary" onClick={toggleCreateAccount}>Create New Account</Button>
                    <Modal  show={showCreateAccount} onHide={toggleCreateAccount}>
                        <Modal.Header closeButton>
                                New Account:
                        </Modal.Header>
                        <Modal.Body>
                            <fieldset>
                                <legend style={ { display: "none"}}>New Account:</legend>
                                <label>Type: <select onChange={handleTypeChange}>
                                    <option selected={(createAccount.type === 'Checking')}>Checking</option>
                                    <option selected={(createAccount.type === 'Savings')}>Savings</option>
                                </select></label>
                                <label>Initial Balance: <input type="text" name="balance" onChange={handleBalanceChange} value={createAccount.balance} /></label>
                            </fieldset>
                        </Modal.Body>
                        <Modal.Footer>
                            <Button variant="secondary" onClick={toggleCreateAccount}>Close</Button>
                            <Button variant="primary" onClick={handleCreateAccount}>Create Account</Button>
                        </Modal.Footer>
                    
                    </Modal>
                    <div>
                        { myaccounts.map((account, i) => {
                            return <Account key={i} account={account} refreshAccounts={refreshAccounts}></Account>
                        })}
                    </div>
                </Col>
            </Row>
        </Container>
    )
}

export default Accounts;