import React from "react";
import ReactDOM from "react-dom";
import { Navbar, Nav, NavItem, NavDropdown, MenuItem } from "react-bootstrap";

export default class Header extends React.Component {
  constructor() {
    super();
  }

  render() {
    return (
      <Navbar fluid="true">
        <Navbar.Header>
          <Navbar.Brand>
            <a href="http://www.andrewlei.org">Home</a>
          </Navbar.Brand>
        </Navbar.Header>
        <Nav>
          <NavItem eventKey={1} href="about.html">About</NavItem>
          <NavDropdown eventKey={2} title="Software" id="basic-nav-dropdown">
            <MenuItem eventKey={2.1} href="plot.html">Finite Element Method Program</MenuItem>
            <MenuItem eventKey={2.2} href="quatdraw.html">Quaternions</MenuItem>
            <MenuItem eventKey={2.3} href="rsa.html">RSA Demonstration</MenuItem>
            <MenuItem eventKey={2.3} href="duanumbers.html">Dual Numbers</MenuItem>
            <MenuItem eventKey={2.3} href="ordermatcher.html">Order Matcher Application</MenuItem>
          </NavDropdown>
        </Nav>
      </Navbar>
    );
  }
}
