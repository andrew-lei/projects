import React from "react";
import ReactDOM from "react-dom";
import { FormControl, Col, Row, Tabs, Tab } from "react-bootstrap";
var $ = require('jquery');

import OrdersPanel from "./OrdersPanel";

class Layout extends React.Component {
  constructor() {
    super();
    this.state = {
      key: 1
    }
  }

  handleSelect(key) {
    console.log('selected ' + key);
    if(key == 2) {
      OrdersPanel.getOrders();
    }
    this.setState({key});
  }

  render() {
    return (
      <Tabs activeKey={this.state.key} onSelect={this.handleSelect.bind(this)} id="AppPanel">
        <Tab eventKey={1} title="Tab 1">
          <OrdersPanel/>
        </Tab>
        <Tab eventKey={2} title="Tab 2">
          <OrdersPanel/>
        </Tab>
      </Tabs>
    );
  }
}

const app = document.getElementById('app');
ReactDOM.render(<Layout/>, app);
