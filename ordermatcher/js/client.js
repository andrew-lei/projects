import React from "react";
import ReactDOM from "react-dom";
import { Tabs, Tab } from "react-bootstrap";

import AddOrderPanel from "./AddOrderPanel";
import OrdersPanel from "./OrdersPanel";
import TransactionsPanel from "./TransactionsPanel";
import Header from "./Header"

class OrderApplication extends React.Component {
  constructor() {
    super();
    this.state = {
      key: 1
    }
  }

  handleSelect(key) {
    if(key == 2) {
      this.refs['orders'].getOrders();
    }
    else if (key == 3) {
      this.refs['transactions'].getTransactions();
    }
    this.setState({key});
  }

  render() {
    return (
      <Tabs activeKey={this.state.key} onSelect={this.handleSelect.bind(this)} id="AppPanel">
        <Tab eventKey={1} title="Add Order">
          <AddOrderPanel/>
        </Tab>
        <Tab eventKey={2} title="View Orders">
          <OrdersPanel ref='orders'/>
        </Tab>
        <Tab eventKey={3} title="View Transactions">
          <TransactionsPanel ref='transactions'/>
        </Tab>
      </Tabs>
    );
  }
}

// const header = document.getElementById('header');
// ReactDOM.render(<Header/>, header);

const app = document.getElementById('app');
ReactDOM.render(<OrderApplication/>, app);
