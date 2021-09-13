import { MsalProvider, AuthenticatedTemplate, UnauthenticatedTemplate } from "@azure/msal-react";

import {Container, Row, Col } from 'react-bootstrap';
import { msalConfig, b2cPolicies, loginRequest } from "./authConfig";

import MsalTest from "./MsalTest";
import './App.css';

import Accounts from "./Accounts"

function App({msalInstance}) {
  
  return (
    <MsalProvider instance={msalInstance}>
      <AuthenticatedTemplate>
        <Container>
          <Row>
            <Col>
              <Accounts></Accounts>
            </Col>
          </Row>
        </Container>
        <MsalTest></MsalTest>
        <pre>

          { JSON.stringify(msalInstance, null, 2) }
        </pre>
        <button onClick={() => msalInstance.logoutRedirect({ postLogoutRedirectUri: "/" })}>Sign out</button>
      </AuthenticatedTemplate>

      <UnauthenticatedTemplate>
        <p>Please sign-in to see your profile information.</p>
        <button onClick={() => msalInstance.loginRedirect(loginRequest)}>Sign in!</button>
      </UnauthenticatedTemplate>
    </MsalProvider>
  );
}

export default App;
