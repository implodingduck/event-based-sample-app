import { MsalProvider, AuthenticatedTemplate, UnauthenticatedTemplate } from "@azure/msal-react";

import {Container, Row, Col, Button, Navbar, Nav } from 'react-bootstrap';
import { msalConfig, b2cPolicies, loginRequest } from "./authConfig";


import MsalTest from "./MsalTest";
import './App.css';

import Accounts from "./Accounts"

function App({msalInstance}) {
  
  return (
    <MsalProvider instance={msalInstance}>
      <div className="App">
      <Navbar bg="light" expand="lg" className="banner">
                <Container>
                    <Navbar.Brand href="/">Quackers Bank</Navbar.Brand>
                    <Navbar.Toggle aria-controls="basic-navbar-nav" />
                    <Navbar.Collapse id="basic-navbar-nav">
                    <Nav className="me-auto">
                      <Nav.Link href="/">Home</Nav.Link>
                      <AuthenticatedTemplate>
                        <Nav.Link onClick={() => msalInstance.logoutRedirect({ postLogoutRedirectUri: "/" })}>Signout</Nav.Link>
                      </AuthenticatedTemplate>
                      <UnauthenticatedTemplate>
                      <Nav.Link onClick={() => msalInstance.loginRedirect(loginRequest)}>Sign in!</Nav.Link>
                      </UnauthenticatedTemplate>
                    </Nav>
                    </Navbar.Collapse>
                </Container>
            </Navbar>
      <AuthenticatedTemplate>
        <Container>
          <Row>
            <Col>
              <MsalTest></MsalTest>
              <Accounts></Accounts>
            </Col>
          </Row>
        </Container>
        {/* <MsalTest></MsalTest>
        <pre>

          { JSON.stringify(msalInstance, null, 2) }
        </pre> */}
        
      </AuthenticatedTemplate>

      <UnauthenticatedTemplate>
        <Container style={ {paddingTop: "1em" }}>
          <Row>
              <Col md={{ span: 6 }}>
                  <div className="box">
                      Quackers Bank is a sample application used to test and demonstrate different technologies. There is no real monetary transactions occuring within this application. Have fun playing around and hopefully you are able to learn something while you are at it. Good Luck, Have Fun and Thanks!
                  </div>
              </Col>
              <Col md={{ span: 6 }}>
                  <div className="box">
                      Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce lacinia tristique metus a rhoncus. Aliquam facilisis gravida turpis, at accumsan ex egestas non. Mauris accumsan risus id iaculis cursus. Cras lectus purus, lobortis id nibh quis, ultrices ultrices ipsum. In quis blandit lacus. Vestibulum gravida, ipsum in faucibus maximus, dui tellus venenatis dolor, eu efficitur magna nibh ornare nisi. Nullam maximus consectetur nunc. Morbi sollicitudin, sapien et consectetur cursus, ipsum eros congue diam, consectetur congue ante velit sit amet erat. In consequat vulputate enim vel porttitor. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nam aliquet turpis quis malesuada mattis. Morbi sed lobortis nulla, a sollicitudin odio. Integer fermentum tempor nisl. Nunc lectus tortor, tempus ut risus eget, scelerisque pellentesque enim. 
                  </div>
              </Col>
          </Row>
      </Container>
      </UnauthenticatedTemplate>
      </div>
    </MsalProvider>
  );
}

export default App;
